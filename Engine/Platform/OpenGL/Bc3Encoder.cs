namespace Engine.Platform.OpenGL;

/// <summary>
/// BC3 (DXT5) block encoder. 16 bytes per 4×4 block, so 1 byte per pixel instead of 4.
/// </summary>
internal static class Bc3Encoder
{
    public static byte[] Encode(byte[] rgba, int width, int height)
    {
        var blocksX = (width + 3) / 4;
        var blocksY = (height + 3) / 4;
        var dst = new byte[blocksX * blocksY * 16];
        for (var by = 0; by < blocksY; by++)
        {
            for (var bx = 0; bx < blocksX; bx++)
                EncodeBlock(rgba, width, height, bx, by, dst.AsSpan((by * blocksX + bx) * 16, 16));
        }

        return dst;
    }

    public static byte[] Half(byte[] src, int width, int height, out int dstWidth, out int dstHeight)
    {
        dstWidth = System.Math.Max(1, width / 2);
        dstHeight = System.Math.Max(1, height / 2);
        var dst = new byte[dstWidth * dstHeight * 4];
        for (var y = 0; y < dstHeight; y++)
        {
            var y0 = System.Math.Min(y * 2, height - 1);
            var y1 = System.Math.Min(y0 + 1, height - 1);
            for (var x = 0; x < dstWidth; x++)
            {
                var x0 = System.Math.Min(x * 2, width - 1);
                var x1 = System.Math.Min(x0 + 1, width - 1);
                var i = (y * dstWidth + x) * 4;
                for (var c = 0; c < 4; c++)
                {
                    dst[i + c] = (byte)((
                        src[(y0 * width + x0) * 4 + c] +
                        src[(y0 * width + x1) * 4 + c] +
                        src[(y1 * width + x0) * 4 + c] +
                        src[(y1 * width + x1) * 4 + c]) / 4);
                }
            }
        }

        return dst;
    }

    private static void EncodeBlock(byte[] rgba, int width, int height, int bx, int by, Span<byte> block)
    {
        Span<byte> r = stackalloc byte[16];
        Span<byte> g = stackalloc byte[16];
        Span<byte> b = stackalloc byte[16];
        Span<byte> a = stackalloc byte[16];
        byte minA = 255, maxA = 0;
        byte minR = 255, minG = 255, minB = 255;
        byte maxR = 0, maxG = 0, maxB = 0;

        for (var py = 0; py < 4; py++)
        {
            for (var px = 0; px < 4; px++)
            {
                Sample(rgba, width, height, bx * 4 + px, by * 4 + py, out var pr, out var pg, out var pb, out var pa);
                var i = py * 4 + px;
                r[i] = pr;
                g[i] = pg;
                b[i] = pb;
                a[i] = pa;
                if (pa < minA) minA = pa;
                if (pa > maxA) maxA = pa;
                if (pr < minR) minR = pr;
                if (pg < minG) minG = pg;
                if (pb < minB) minB = pb;
                if (pr > maxR) maxR = pr;
                if (pg > maxG) maxG = pg;
                if (pb > maxB) maxB = pb;
            }
        }

        if (maxA <= minA)
        {
            if (maxA < 255) maxA++;
            else minA--;
        }

        WriteAlpha(block, a, maxA, minA);
        WriteColor(block[8..], r, g, b, maxR, maxG, maxB, minR, minG, minB);
    }

    private static void Sample(byte[] rgba, int width, int height, int x, int y,
        out byte r, out byte g, out byte b, out byte a)
    {
        if (x >= width) x = width - 1;
        if (y >= height) y = height - 1;
        var i = (y * width + x) * 4;
        r = rgba[i];
        g = rgba[i + 1];
        b = rgba[i + 2];
        a = rgba[i + 3];
    }

    private static void WriteAlpha(Span<byte> block, ReadOnlySpan<byte> alpha, byte a0, byte a1)
    {
        block[0] = a0;
        block[1] = a1;
        Span<byte> palette = stackalloc byte[8];
        palette[0] = a0;
        palette[1] = a1;
        if (a0 > a1)
        {
            palette[2] = (byte)((6 * a0 + 1 * a1) / 7);
            palette[3] = (byte)((5 * a0 + 2 * a1) / 7);
            palette[4] = (byte)((4 * a0 + 3 * a1) / 7);
            palette[5] = (byte)((3 * a0 + 4 * a1) / 7);
            palette[6] = (byte)((2 * a0 + 5 * a1) / 7);
            palette[7] = (byte)((1 * a0 + 6 * a1) / 7);
        }
        else
        {
            palette[2] = (byte)((4 * a0 + 1 * a1) / 5);
            palette[3] = (byte)((3 * a0 + 2 * a1) / 5);
            palette[4] = (byte)((2 * a0 + 3 * a1) / 5);
            palette[5] = (byte)((1 * a0 + 4 * a1) / 5);
            palette[6] = 0;
            palette[7] = 255;
        }

        ulong bits = 0;
        for (var i = 0; i < 16; i++)
            bits |= (ulong)Nearest(alpha[i], palette) << (i * 3);

        for (var i = 0; i < 6; i++)
            block[2 + i] = (byte)(bits >> (i * 8));
    }

    private static void WriteColor(Span<byte> block, ReadOnlySpan<byte> r, ReadOnlySpan<byte> g, ReadOnlySpan<byte> b,
        byte maxR, byte maxG, byte maxB, byte minR, byte minG, byte minB)
    {
        var c0 = To565(maxR, maxG, maxB);
        var c1 = To565(minR, minG, minB);
        if (c0 == c1)
            c0 = (ushort)(c0 == 0 ? 1 : c0 - 1);
        if (c0 < c1)
            (c0, c1) = (c1, c0);

        block[0] = (byte)c0;
        block[1] = (byte)(c0 >> 8);
        block[2] = (byte)c1;
        block[3] = (byte)(c1 >> 8);

        From565(c0, out var r0, out var g0, out var b0);
        From565(c1, out var r1, out var g1, out var b1);
        Span<byte> pr = stackalloc byte[4];
        Span<byte> pg = stackalloc byte[4];
        Span<byte> pb = stackalloc byte[4];
        pr[0] = r0; pg[0] = g0; pb[0] = b0;
        pr[1] = r1; pg[1] = g1; pb[1] = b1;
        pr[2] = (byte)((2 * r0 + r1) / 3); pg[2] = (byte)((2 * g0 + g1) / 3); pb[2] = (byte)((2 * b0 + b1) / 3);
        pr[3] = (byte)((r0 + 2 * r1) / 3); pg[3] = (byte)((g0 + 2 * g1) / 3); pb[3] = (byte)((b0 + 2 * b1) / 3);

        uint bits = 0;
        for (var i = 0; i < 16; i++)
        {
            var best = 0;
            var bestDist = int.MaxValue;
            for (var p = 0; p < 4; p++)
            {
                var dr = r[i] - pr[p];
                var dg = g[i] - pg[p];
                var db = b[i] - pb[p];
                var dist = dr * dr + dg * dg + db * db;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = p;
                }
            }

            bits |= (uint)best << (i * 2);
        }

        block[4] = (byte)bits;
        block[5] = (byte)(bits >> 8);
        block[6] = (byte)(bits >> 16);
        block[7] = (byte)(bits >> 24);
    }

    private static int Nearest(byte value, ReadOnlySpan<byte> palette)
    {
        var best = 0;
        var bestDist = int.MaxValue;
        for (var i = 0; i < palette.Length; i++)
        {
            var dist = System.Math.Abs(value - palette[i]);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = i;
            }
        }

        return best;
    }

    private static ushort To565(byte r, byte g, byte b) =>
        (ushort)(((r >> 3) << 11) | ((g >> 2) << 5) | (b >> 3));

    private static void From565(ushort c, out byte r, out byte g, out byte b)
    {
        var r5 = (c >> 11) & 31;
        var g6 = (c >> 5) & 63;
        var b5 = c & 31;
        r = (byte)((r5 << 3) | (r5 >> 2));
        g = (byte)((g6 << 2) | (g6 >> 4));
        b = (byte)((b5 << 3) | (b5 >> 2));
    }
}
