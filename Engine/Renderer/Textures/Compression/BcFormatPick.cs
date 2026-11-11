namespace Engine.Renderer.Textures.Compression;

internal static class BcFormatPick
{
    public static BcGpuFormat Pick(bool sRgb, bool isNormalMap, ReadOnlySpan<byte> rgba, int width, int height)
    {
        if (sRgb)
            return HasAlpha(rgba, width, height) ? BcGpuFormat.Dxt5Srgb : BcGpuFormat.Dxt1Srgb;
        if (isNormalMap)
            return BcGpuFormat.Bc5;
        if (IsSingleChannel(rgba, width, height))
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

    private static bool IsSingleChannel(ReadOnlySpan<byte> rgba, int width, int height)
    {
        var pixels = width * height;
        for (var i = 0; i < pixels; i++)
        {
            var o = i * 4;
            var r = rgba[o];
            var g = rgba[o + 1];
            var b = rgba[o + 2];
            if (r != g || r != b)
                return false;
        }

        return true;
    }
}
