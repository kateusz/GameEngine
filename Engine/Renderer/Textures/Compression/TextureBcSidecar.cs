namespace Engine.Renderer.Textures.Compression;

public static class TextureBcSidecar
{
    public static bool TryEnsureUpToDate(string sourcePath, out string? error)
    {
        error = null;
        var normalized = Path.GetFullPath(sourcePath);
        if (!TextureBcSource.IsRasterSourceExtension(normalized))
            return true;

        if (!File.Exists(normalized))
        {
            error = $"Texture source not found: {normalized}";
            return false;
        }

        var (sRgb, isNormal) = TextureBcSource.InferEncodeRole(normalized);
        var isOcclusion = TextureBcSource.IsOcclusionMap(normalized);
        var sidecar = BcDdsSidecarPath.ForSource(normalized, sRgb);
        if (SidecarUsable(normalized, sidecar, sRgb, isNormal))
            return true;

        byte[] rgba;
        int width;
        int height;
        try
        {
            (rgba, width, height) = BcTextureEncoder.DecodeToRgba(normalized, sRgb);
        }
        catch (Exception ex)
        {
            error = $"Failed to decode {normalized}: {ex.Message}";
            return false;
        }

        var format = BcFormatPick.Pick(sRgb, isNormal, isOcclusion, rgba, width, height);
        var (image, dds) = BcTextureEncoder.EncodeWithDds(rgba, width, height, format, sRgb, isNormal);
        if (image == null || dds == null)
        {
            error = $"BC encode failed for {normalized}";
            return false;
        }

        if (!BcDds.Write(sidecar, image.Value, dds))
        {
            error = $"Failed to write DDS sibling for {normalized}";
            return false;
        }

        return true;
    }

    internal static bool SidecarUsable(string sourcePath, string sidecarPath, bool sRgb, bool isNormal)
    {
        if (!File.Exists(sidecarPath)
            || File.GetLastWriteTimeUtc(sidecarPath) < File.GetLastWriteTimeUtc(sourcePath)
            || !BcDds.TryRead(sidecarPath, out var bc))
            return false;

        return bc.SRgb == sRgb && bc.IsNormal == isNormal;
    }
}
