using Pfim;
using StbImageSharp;
using Buffer = System.Buffer;
using InternalFormat = Silk.NET.OpenGL.InternalFormat;
using PixelFormat = Silk.NET.OpenGL.PixelFormat;

namespace Engine.Platform.OpenGL;

internal static class TextureFileDecoder
{
    private const int StbiFlipVerticallyEnabled = 1;

    private static readonly HashSet<string> PfimExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".dds", ".tga"
    };

    internal readonly record struct DecodedImage(
        byte[] Data,
        int Width,
        int Height,
        InternalFormat InternalFormat,
        PixelFormat DataFormat);

    public static DecodedImage Decode(string path, bool sRgb)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Texture file not found: {path}", path);

        var ext = Path.GetExtension(path);
        return PfimExtensions.Contains(ext) ? DecodePfim(path, sRgb) : DecodeStb(path, sRgb);
    }

    private static DecodedImage DecodeStb(string path, bool sRgb)
    {
        StbImage.stbi_set_flip_vertically_on_load(StbiFlipVerticallyEnabled);

        ImageResult image;
        using (var stream = File.OpenRead(path))
            image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

        var internalFormat = sRgb ? InternalFormat.Srgb8Alpha8 : InternalFormat.Rgba8;
        return new DecodedImage(image.Data, image.Width, image.Height, internalFormat, PixelFormat.Rgba);
    }

    private static DecodedImage DecodePfim(string path, bool sRgb)
    {
        using var pfimImage = Pfimage.FromFile(path);
        if (pfimImage.Compressed)
            pfimImage.Decompress();

        var (internalFormat, dataFormat) = pfimImage.Format switch
        {
            ImageFormat.Rgba32 => (sRgb ? InternalFormat.Srgb8Alpha8 : InternalFormat.Rgba8, PixelFormat.Bgra),
            ImageFormat.Rgb24 => (sRgb ? InternalFormat.Srgb8 : InternalFormat.Rgb8, PixelFormat.Bgr),
            ImageFormat.R5g5b5 => (InternalFormat.Rgb5, PixelFormat.Bgr),
            ImageFormat.R5g6b5 => (InternalFormat.Rgb565, PixelFormat.Bgr),
            ImageFormat.R5g5b5a1 => (InternalFormat.Rgb5A1, PixelFormat.Bgra),
            ImageFormat.Rgba16 => (InternalFormat.Rgba4, PixelFormat.Bgra),
            _ => throw new NotSupportedException($"Unsupported Pfim format '{pfimImage.Format}' for texture: {path}")
        };

        var bytesPerPixel = pfimImage.BitsPerPixel / 8;
        if (bytesPerPixel == 0)
            throw new NotSupportedException(
                $"Pfim reported BitsPerPixel=0 for '{pfimImage.Format}' in texture: {path}");

        var tightStride = pfimImage.Width * bytesPerPixel;
        byte[] data;

        if (pfimImage.Stride != tightStride)
        {
            data = new byte[tightStride * pfimImage.Height];
            for (var row = 0; row < pfimImage.Height; row++)
                Buffer.BlockCopy(pfimImage.Data, row * pfimImage.Stride, data, row * tightStride, tightStride);
        }
        else
        {
            data = pfimImage.Data;
        }

        FlipVertically(data, pfimImage.Height, tightStride);
        return new DecodedImage(data, pfimImage.Width, pfimImage.Height, internalFormat, dataFormat);
    }

    private static void FlipVertically(byte[] data, int height, int stride)
    {
        var tempRow = new byte[stride];
        for (var y = 0; y < height / 2; y++)
        {
            var topOffset = y * stride;
            var bottomOffset = (height - 1 - y) * stride;
            Buffer.BlockCopy(data, topOffset, tempRow, 0, stride);
            Buffer.BlockCopy(data, bottomOffset, data, topOffset, stride);
            Buffer.BlockCopy(tempRow, 0, data, bottomOffset, stride);
        }
    }
}
