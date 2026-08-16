using System.Numerics;
using System.Text;

namespace Engine.GraphicsTests;

internal static class HdrTestImages
{
    /// <summary>Writes an uncompressed radiance .hdr filled with one color. Keep width &lt; 8.</summary>
    public static void WriteSolidHdr(string path, int width, int height, Vector3 color)
    {
        using var fs = File.Create(path);
        var header = $"#?RADIANCE\nFORMAT=32-bit_rle_rgbe\n\n-Y {height} +X {width}\n";
        fs.Write(Encoding.ASCII.GetBytes(header));

        var rgbe = EncodeRgbe(color);
        for (var i = 0; i < width * height; i++)
            fs.Write(rgbe);
    }

    /// <summary>
    /// Blue sky with a small warm sun in the first scanline (file top / typical zenith).
    /// Used to lock: irradiance must keep the sun's warmth, not clamp it away.
    /// </summary>
    public static void WriteBlueSkyWithWarmSun(string path, int width, int height)
    {
        using var fs = File.Create(path);
        var header = $"#?RADIANCE\nFORMAT=32-bit_rle_rgbe\n\n-Y {height} +X {width}\n";
        fs.Write(Encoding.ASCII.GetBytes(header));

        var sky = EncodeRgbe(new Vector3(0.15f, 0.25f, 1.2f));
        var sun = EncodeRgbe(new Vector3(200f, 160f, 40f));
        var sunWidth = System.Math.Max(1, width / 8);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var isSun = y == 0 && x < sunWidth;
                fs.Write(isSun ? sun : sky);
            }
        }
    }

    private static byte[] EncodeRgbe(Vector3 c)
    {
        var max = MathF.Max(c.X, MathF.Max(c.Y, c.Z));
        if (max < 1e-32f)
            return [0, 0, 0, 0];

        var e = (int)MathF.Floor(MathF.Log2(max)) + 1;
        var scale = 256f / MathF.Pow(2f, e);
        return [(byte)(c.X * scale), (byte)(c.Y * scale), (byte)(c.Z * scale), (byte)(e + 128)];
    }
}
