namespace Engine.Renderer.Textures.Compression;

internal static class BcFormatPick
{
    public static BcGpuFormat Pick(bool sRgb, bool isNormalMap, bool isOcclusionMap, ReadOnlySpan<byte> rgba,
        int width, int height)
    {
        if (sRgb)
            return HasAlpha(rgba, width, height) ? BcGpuFormat.Dxt5Srgb : BcGpuFormat.Dxt1Srgb;
        if (isNormalMap)
            return BcGpuFormat.Bc5;
        if (isOcclusionMap)
            return BcGpuFormat.Bc4;
        return BcGpuFormat.Dxt5Linear;
    }

    public static bool UsesS3Tc(BcGpuFormat format) =>
        format is BcGpuFormat.Dxt1Srgb or BcGpuFormat.Dxt5Srgb or BcGpuFormat.Dxt5Linear;

    private static bool HasAlpha(ReadOnlySpan<byte> rgba, int width, int height)
    {
        var pixels = width * height;
        for (var i = 0; i < pixels; i++)
        {
            if (rgba[i * 4 + 3] < 255)
                return true;
        }

        return false;
    }
}
