namespace Engine.Renderer.Textures.Compression;

public static class TextureBcCook
{
    public static bool TryCookFile(string sourcePath, out string? error)
    {
        error = null;
        var normalized = Path.GetFullPath(sourcePath);
        if (!TextureCookInference.IsCookableExtension(normalized))
            return true;

        if (!File.Exists(normalized))
        {
            error = $"Texture source not found: {normalized}";
            return false;
        }

        var (sRgb, isNormal) = TextureCookInference.InferCookRole(normalized);
        var sidecar = Path.ChangeExtension(normalized, ".dds");
        if (File.Exists(sidecar)
            && File.GetLastWriteTimeUtc(sidecar) >= File.GetLastWriteTimeUtc(normalized)
            && BcDds.TryRead(sidecar, out _))
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

        var format = BcFormatPick.Pick(sRgb, isNormal, rgba, width, height);
        var (image, dds) = BcTextureEncoder.EncodeWithDds(rgba, width, height, format);
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
}
