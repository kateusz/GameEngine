using Pfim;
using InternalFormat = Silk.NET.OpenGL.InternalFormat;
using PixelFormat = Silk.NET.OpenGL.PixelFormat;

namespace Engine.Platform.OpenGL;

internal static class TextureRasterToRgba8
{
    public static (byte[] Rgba, int Width, int Height) Decode(string path, bool sRgb)
    {
        var decoded = TextureFileDecoder.Decode(path, sRgb);
        var rgba = ToRgba8(decoded);
        return (rgba, decoded.Width, decoded.Height);
    }

    private static byte[] ToRgba8(TextureFileDecoder.DecodedImage decoded)
    {
        var pixels = decoded.Width * decoded.Height;
        if (pixels == 0)
            throw new InvalidOperationException($"Empty texture: {decoded.Width}x{decoded.Height}");

        var bpp = decoded.Data.Length / pixels;
        return (decoded.DataFormat, bpp, decoded.InternalFormat) switch
        {
            (PixelFormat.Bgra, 4, _) => TexturePreviewScaling.ToPackedRgba(decoded.Data, 4),
            (PixelFormat.Bgr, 3, _) => TexturePreviewScaling.ToPackedRgba(decoded.Data, 3),
            (PixelFormat.Bgr, 2, InternalFormat.Rgb565) => ExpandRgb565(decoded.Data, pixels),
            (PixelFormat.Bgr, 2, InternalFormat.Rgb5) => ExpandRgb555(decoded.Data, pixels),
            (PixelFormat.Bgra, 2, InternalFormat.Rgb5A1) => ExpandRgb5A1(decoded.Data, pixels),
            (PixelFormat.Bgra, 8, InternalFormat.Rgba4) => ExpandRgba64ToRgba8(decoded.Data, pixels),
            _ => throw new NotSupportedException(
                $"Cannot encode texture with format {decoded.DataFormat}, {bpp} bpp, internal {decoded.InternalFormat} to RGBA8.")
        };
    }

    private static byte[] ExpandRgb565(ReadOnlySpan<byte> data, int pixels)
    {
        var rgba = new byte[pixels * 4];
        for (var i = 0; i < pixels; i++)
        {
            var packed = (ushort)(data[i * 2] | (data[i * 2 + 1] << 8));
            var r = (byte)((packed >> 11) & 0x1F);
            var g = (byte)((packed >> 5) & 0x3F);
            var b = (byte)(packed & 0x1F);
            var o = i * 4;
            rgba[o] = Scale5(r);
            rgba[o + 1] = Scale6(g);
            rgba[o + 2] = Scale5(b);
            rgba[o + 3] = 255;
        }

        return rgba;
    }

    private static byte[] ExpandRgb555(ReadOnlySpan<byte> data, int pixels)
    {
        var rgba = new byte[pixels * 4];
        for (var i = 0; i < pixels; i++)
        {
            var packed = (ushort)(data[i * 2] | (data[i * 2 + 1] << 8));
            var r = (byte)((packed >> 10) & 0x1F);
            var g = (byte)((packed >> 5) & 0x1F);
            var b = (byte)(packed & 0x1F);
            var o = i * 4;
            rgba[o] = Scale5(r);
            rgba[o + 1] = Scale5(g);
            rgba[o + 2] = Scale5(b);
            rgba[o + 3] = 255;
        }

        return rgba;
    }

    private static byte[] ExpandRgb5A1(ReadOnlySpan<byte> data, int pixels)
    {
        var rgba = new byte[pixels * 4];
        for (var i = 0; i < pixels; i++)
        {
            var packed = (ushort)(data[i * 2] | (data[i * 2 + 1] << 8));
            var a = (packed & 0x8000) != 0 ? (byte)255 : (byte)0;
            var r = (byte)((packed >> 10) & 0x1F);
            var g = (byte)((packed >> 5) & 0x1F);
            var b = (byte)(packed & 0x1F);
            var o = i * 4;
            rgba[o] = Scale5(r);
            rgba[o + 1] = Scale5(g);
            rgba[o + 2] = Scale5(b);
            rgba[o + 3] = a;
        }

        return rgba;
    }

    private static byte[] ExpandRgba64ToRgba8(ReadOnlySpan<byte> data, int pixels)
    {
        var rgba = new byte[pixels * 4];
        for (var i = 0; i < pixels; i++)
        {
            var o = i * 8;
            rgba[i * 4] = (byte)(data[o + 1]);
            rgba[i * 4 + 1] = (byte)(data[o + 3]);
            rgba[i * 4 + 2] = (byte)(data[o + 5]);
            rgba[i * 4 + 3] = (byte)(data[o + 7]);
        }

        return rgba;
    }

    private static byte Scale5(int v) => (byte)((v * 255 + 15) / 31);
    private static byte Scale6(int v) => (byte)((v * 255 + 31) / 63);
}
