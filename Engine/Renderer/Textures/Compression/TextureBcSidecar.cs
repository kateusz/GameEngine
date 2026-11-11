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
        if (TryReadUsableSidecar(normalized, sidecar, sRgb, isNormal, out _))
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

        if (!CanEncodeWithoutPadding(width, height))
            return true;

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

    internal static bool TryReadUsableSidecar(
        string sourcePath, string sidecarPath, bool sRgb, bool isNormal, out BcImage image)
    {
        image = default;
        if (!File.Exists(sidecarPath)
            || File.GetLastWriteTimeUtc(sidecarPath) < File.GetLastWriteTimeUtc(sourcePath)
            || !BcDds.TryRead(sidecarPath, out var bc))
            return false;

        if (bc.SRgb != sRgb || bc.IsNormal != isNormal
            || bc.SourceWidth != bc.GpuWidth || bc.SourceHeight != bc.GpuHeight)
            return false;

        image = bc;
        return true;
    }

    // Repeat uses the uploaded dimensions. Use the original raster when padding would change the repeat period.
    internal static bool CanEncodeWithoutPadding(int width, int height) =>
        width % 4 == 0 && height % 4 == 0;
}
