namespace Engine.Platform.OpenGL;

internal static class TexturePreviewScaling
{
    public static (byte[] Data, int Width, int Height) DownscaleRgba(byte[] src, int srcWidth, int srcHeight,
        int maxEdge)
    {
        if (srcWidth <= 0 || srcHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(srcWidth), "Source dimensions must be positive.");

        var maxDim = System.Math.Max(srcWidth, srcHeight);
        if (maxDim <= maxEdge)
            return (src, srcWidth, srcHeight);

        var dstWidth = System.Math.Max(1, srcWidth * maxEdge / maxDim);
        var dstHeight = System.Math.Max(1, srcHeight * maxEdge / maxDim);
        var dst = new byte[dstWidth * dstHeight * 4];

        for (var y = 0; y < dstHeight; y++)
        {
            var sy = y * srcHeight / dstHeight;
            var rowOffset = sy * srcWidth * 4;
            for (var x = 0; x < dstWidth; x++)
            {
                var si = rowOffset + x * srcWidth / dstWidth * 4;
                var di = (y * dstWidth + x) * 4;
                dst[di] = src[si];
                dst[di + 1] = src[si + 1];
                dst[di + 2] = src[si + 2];
                dst[di + 3] = src[si + 3];
            }
        }

        return (dst, dstWidth, dstHeight);
    }
}
