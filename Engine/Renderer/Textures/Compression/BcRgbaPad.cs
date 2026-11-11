namespace Engine.Renderer.Textures.Compression;

internal static class BcRgbaPad
{
    public static (byte[] Padded, int GpuWidth, int GpuHeight) Pad(ReadOnlySpan<byte> rgba, int width, int height)
    {
        var gpuWidth = (width + 3) / 4 * 4;
        var gpuHeight = (height + 3) / 4 * 4;
        if (gpuWidth == width && gpuHeight == height)
            return (rgba.ToArray(), gpuWidth, gpuHeight);

        var padded = new byte[gpuWidth * gpuHeight * 4];
        for (var y = 0; y < gpuHeight; y++)
        {
            var srcY = System.Math.Min(y, height - 1);
            for (var x = 0; x < gpuWidth; x++)
            {
                var srcX = System.Math.Min(x, width - 1);
                var src = (srcY * width + srcX) * 4;
                var dst = (y * gpuWidth + x) * 4;
                padded[dst] = rgba[src];
                padded[dst + 1] = rgba[src + 1];
                padded[dst + 2] = rgba[src + 2];
                padded[dst + 3] = rgba[src + 3];
            }
        }

        return (padded, gpuWidth, gpuHeight);
    }
}
