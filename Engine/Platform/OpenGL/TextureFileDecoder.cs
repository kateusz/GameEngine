using Pfim;
using StbImageSharp;
using Buffer = System.Buffer;
using InternalFormat = Silk.NET.OpenGL.InternalFormat;
using PixelFormat = Silk.NET.OpenGL.PixelFormat;

namespace Engine.Platform.OpenGL;

internal static class TextureFileDecoder
{
    private static readonly Lock DecodeLock = new();
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

        lock (DecodeLock)
        {
            var ext = Path.GetExtension(path);
            return PfimExtensions.Contains(ext) ? DecodePfim(path, sRgb) : DecodeStb(path, sRgb);
        }
    }

    private static DecodedImage DecodeStb(string path, bool sRgb)
    {
        StbImage.stbi_set_flip_vertically_on_load(StbiFlipVerticallyEnabled);

        ImageResult image;
        using (var stream = File.OpenRead(path))
            image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

        if (sRgb)
            BleedTransparentRgb(image.Data, image.Width, image.Height);

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

    // ponytail: spreads opaque RGB into alpha<0.5 texels so mipmaps don't average in a white background.
    // Alpha is unchanged. Worst case is a few full-image passes at load.
    internal static void BleedTransparentRgb(byte[] rgba, int width, int height)
    {
        var count = width * height;
        if (width <= 0 || height <= 0 || rgba.Length < count * 4)
            return;

        var filled = new byte[count];
        var anyTransparent = false;
        for (var i = 0; i < count; i++)
        {
            if (rgba[i * 4 + 3] >= 128)
                filled[i] = 1;
            else
                anyTransparent = true;
        }

        if (!anyTransparent)
            return;

        var nextFilled = new byte[count];
        ReadOnlySpan<(int X, int Y)> offsets =
        [
            (-1, 0), (1, 0), (0, -1), (0, 1),
            (-1, -1), (1, -1), (-1, 1), (1, 1)
        ];
        for (var step = 1; step < System.Math.Max(width, height); step <<= 1)
        {
            filled.AsSpan().CopyTo(nextFilled);
            var changed = false;
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var i = y * width + x;
                if (filled[i] != 0)
                    continue;
                foreach (var (ox, oy) in offsets)
                {
                    var nx = x + ox * step;
                    var ny = y + oy * step;
                    if ((uint)nx >= (uint)width || (uint)ny >= (uint)height)
                        continue;
                    var n = ny * width + nx;
                    if (filled[n] == 0)
                        continue;
                    var dst = i * 4;
                    var src = n * 4;
                    rgba[dst] = rgba[src];
                    rgba[dst + 1] = rgba[src + 1];
                    rgba[dst + 2] = rgba[src + 2];
                    nextFilled[i] = 1;
                    changed = true;
                    break;
                }
            }
            (filled, nextFilled) = (nextFilled, filled);
            if (!changed)
                break;
        }
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
