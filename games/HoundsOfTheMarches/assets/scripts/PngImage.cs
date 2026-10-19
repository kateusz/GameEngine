using System.IO.Compression;

namespace Hounds.Marches;

public readonly record struct Rgba(byte R, byte G, byte B, byte A = 255);

public sealed class Raster
{
    public int W { get; }
    public int H { get; }
    public byte[] P { get; }

    public Raster(int w, int h)
    {
        W = w;
        H = h;
        P = new byte[w * h * 4];
    }

    public void Clear(Rgba c)
    {
        for (var i = 0; i < P.Length; i += 4)
        {
            P[i] = c.R;
            P[i + 1] = c.G;
            P[i + 2] = c.B;
            P[i + 3] = c.A;
        }
    }

    public void Pix(int x, int y, Rgba c)
    {
        if ((uint)x >= (uint)W || (uint)y >= (uint)H || c.A == 0)
            return;
        var i = (y * W + x) * 4;
        if (c.A == 255 || P[i + 3] == 0)
        {
            P[i] = c.R;
            P[i + 1] = c.G;
            P[i + 2] = c.B;
            P[i + 3] = c.A;
            return;
        }

        var a = c.A / 255f;
        var ia = 1f - a;
        P[i] = (byte)(c.R * a + P[i] * ia);
        P[i + 1] = (byte)(c.G * a + P[i + 1] * ia);
        P[i + 2] = (byte)(c.B * a + P[i + 2] * ia);
        P[i + 3] = (byte)System.Math.Min(255, P[i + 3] + c.A);
    }

    public void Rect(int x, int y, int w, int h, Rgba c)
    {
        for (var yy = y; yy < y + h; yy++)
            for (var xx = x; xx < x + w; xx++)
                Pix(xx, yy, c);
    }

    public void Ellipse(int cx, int cy, int rx, int ry, Rgba c)
    {
        if (rx <= 0 || ry <= 0)
            return;
        var rx2 = rx * rx;
        var ry2 = ry * ry;
        for (var y = -ry; y <= ry; y++)
        {
            for (var x = -rx; x <= rx; x++)
            {
                if (x * x * ry2 + y * y * rx2 <= rx2 * ry2)
                    Pix(cx + x, cy + y, c);
            }
        }
    }

    public void Blob(int cx, int cy, int rx, int ry, Rgba light, Rgba dark)
    {
        Ellipse(cx + 3, cy + 5, rx, ry, dark);
        Ellipse(cx, cy, rx, ry, light);
    }

    public void Box(int x, int y, int w, int h, Rgba light, Rgba dark)
    {
        Rect(x + 3, y + 4, w, h, dark);
        Rect(x, y, w, h, light);
    }

    public void Line(int x0, int y0, int x1, int y1, int radius, Rgba c)
    {
        var dx = x1 - x0;
        var dy = y1 - y0;
        var steps = System.Math.Max(System.Math.Abs(dx), System.Math.Abs(dy));
        if (steps == 0)
        {
            Ellipse(x0, y0, radius, radius, c);
            return;
        }

        for (var i = 0; i <= steps; i += System.Math.Max(1, radius / 2))
        {
            var t = i / (float)steps;
            Ellipse(x0 + (int)(dx * t), y0 + (int)(dy * t), radius, radius, c);
        }
        Ellipse(x1, y1, radius, radius, c);
    }

    public void Tri(int x0, int y0, int x1, int y1, int x2, int y2, Rgba c)
    {
        var minX = System.Math.Clamp(System.Math.Min(x0, System.Math.Min(x1, x2)), 0, W - 1);
        var maxX = System.Math.Clamp(System.Math.Max(x0, System.Math.Max(x1, x2)), 0, W - 1);
        var minY = System.Math.Clamp(System.Math.Min(y0, System.Math.Min(y1, y2)), 0, H - 1);
        var maxY = System.Math.Clamp(System.Math.Max(y0, System.Math.Max(y1, y2)), 0, H - 1);
        var area = Edge(x0, y0, x1, y1, x2, y2);
        if (area == 0)
            return;
        for (var y = minY; y <= maxY; y++)
        {
            for (var x = minX; x <= maxX; x++)
            {
                var w0 = Edge(x1, y1, x2, y2, x, y);
                var w1 = Edge(x2, y2, x0, y0, x, y);
                var w2 = Edge(x0, y0, x1, y1, x, y);
                if (w0 >= 0 && w1 >= 0 && w2 >= 0 || w0 <= 0 && w1 <= 0 && w2 <= 0)
                    Pix(x, y, c);
            }
        }
    }

    private static int Edge(int ax, int ay, int bx, int by, int cx, int cy) =>
        (cx - ax) * (by - ay) - (cy - ay) * (bx - ax);

    public void Outline(int x, int y, int w, int h, int radius, Rgba ink)
    {
        var mark = new bool[w * h];
        for (var yy = 0; yy < h; yy++)
        {
            for (var xx = 0; xx < w; xx++)
            {
                var px = x + xx;
                var py = y + yy;
                if ((uint)px >= (uint)W || (uint)py >= (uint)H)
                    continue;
                if (P[(py * W + px) * 4 + 3] == 0)
                    continue;
                var r2 = radius * radius;
                for (var dy = -radius; dy <= radius; dy++)
                {
                    var iy = yy + dy;
                    if ((uint)iy >= (uint)h)
                        continue;
                    for (var dx = -radius; dx <= radius; dx++)
                    {
                        if (dx * dx + dy * dy > r2)
                            continue;
                        var ix = xx + dx;
                        if ((uint)ix >= (uint)w)
                            continue;
                        mark[iy * w + ix] = true;
                    }
                }
            }
        }

        for (var i = 0; i < mark.Length; i++)
        {
            if (!mark[i])
                continue;
            var px = x + i % w;
            var py = y + i / w;
            var o = (py * W + px) * 4;
            if (P[o + 3] == 0)
            {
                P[o] = ink.R;
                P[o + 1] = ink.G;
                P[o + 2] = ink.B;
                P[o + 3] = ink.A;
            }
        }
    }

    public void Save(string path) => PngImage.Write(path, W, H, P);
}

public static class PngImage
{
    public static void Write(string path, int w, int h, byte[] rgba)
    {
        var raw = new byte[(w * 4 + 1) * h];
        for (var y = 0; y < h; y++)
        {
            var row = y * (w * 4 + 1);
            raw[row] = 0;
            Buffer.BlockCopy(rgba, y * w * 4, raw, row + 1, w * 4);
        }

        using var ms = new MemoryStream();
        ms.WriteByte(0x78);
        ms.WriteByte(0x9C);
        using (var deflate = new DeflateStream(ms, CompressionLevel.Optimal, leaveOpen: true))
            deflate.Write(raw, 0, raw.Length);
        var adler = Adler(raw);
        ms.WriteByte((byte)(adler >> 24));
        ms.WriteByte((byte)(adler >> 16));
        ms.WriteByte((byte)(adler >> 8));
        ms.WriteByte((byte)adler);
        var idat = ms.ToArray();

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var fs = File.Create(path);
        fs.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        WriteChunk(fs, "IHDR", Ihdr(w, h));
        WriteChunk(fs, "IDAT", idat);
        WriteChunk(fs, "IEND", []);
    }

    private static byte[] Ihdr(int w, int h)
    {
        var data = new byte[13];
        WriteBe(data, 0, w);
        WriteBe(data, 4, h);
        data[8] = 8;
        data[9] = 6;
        return data;
    }

    private static void WriteChunk(Stream fs, string type, byte[] data)
    {
        var len = new byte[4];
        WriteBe(len, 0, data.Length);
        fs.Write(len);
        var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
        fs.Write(typeBytes);
        fs.Write(data);
        var crcSrc = new byte[typeBytes.Length + data.Length];
        Buffer.BlockCopy(typeBytes, 0, crcSrc, 0, typeBytes.Length);
        Buffer.BlockCopy(data, 0, crcSrc, typeBytes.Length, data.Length);
        var crc = new byte[4];
        WriteBe(crc, 0, (int)Crc(crcSrc));
        fs.Write(crc);
    }

    private static void WriteBe(byte[] data, int offset, int value)
    {
        data[offset] = (byte)(value >> 24);
        data[offset + 1] = (byte)(value >> 16);
        data[offset + 2] = (byte)(value >> 8);
        data[offset + 3] = (byte)value;
    }

    private static uint Crc(byte[] data)
    {
        var c = 0xFFFFFFFFu;
        foreach (var b in data)
        {
            c ^= b;
            for (var i = 0; i < 8; i++)
                c = (c & 1) != 0 ? (c >> 1) ^ 0xEDB88320u : c >> 1;
        }
        return c ^ 0xFFFFFFFFu;
    }

    private static uint Adler(byte[] data)
    {
        uint a = 1, b = 0;
        foreach (var by in data)
        {
            a = (a + by) % 65521;
            b = (b + a) % 65521;
        }
        return (b << 16) | a;
    }
}

public static class WavFile
{
    public static void WriteTone(string path, float freq, float seconds, float volume, bool noise, int seed)
    {
        const int rate = 22050;
        var n = System.Math.Max(1, (int)(rate * seconds));
        var samples = new short[n];
        var rng = new Random(seed);
        for (var i = 0; i < n; i++)
        {
            var t = i / (float)rate;
            var env = MathF.Pow(1f - t / seconds, 0.6f);
            var s = noise
                ? rng.NextSingle() * 2f - 1f
                : MathF.Sign(MathF.Sin(MathF.Tau * freq * t));
            samples[i] = (short)(System.Math.Clamp(s * env * volume, -1f, 1f) * 16000);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var fs = File.Create(path);
        using var bw = new BinaryWriter(fs);
        var dataBytes = n * 2;
        bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        bw.Write(36 + dataBytes);
        bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
        bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        bw.Write(16);
        bw.Write((short)1);
        bw.Write((short)1);
        bw.Write(rate);
        bw.Write(rate * 2);
        bw.Write((short)2);
        bw.Write((short)16);
        bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        bw.Write(dataBytes);
        foreach (var s in samples)
            bw.Write(s);
    }
}
