using StbImageSharp;

namespace Engine.Renderer.Textures;

/// <summary>
/// Decodes Radiance .hdr equirectangular images to float RGBA (A = 1 when source is RGB).
/// </summary>
internal static class HdrEquirectDecoder
{
    private const int StbiFlipVerticallyEnabled = 1;

    internal static HdrEquirectImage Decode(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"HDR file not found: {path}", path);

        StbImage.stbi_set_flip_vertically_on_load(StbiFlipVerticallyEnabled);

        ImageResultFloat image;
        try
        {
            using var stream = File.OpenRead(path);
            image = ImageResultFloat.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"Failed to decode HDR image: {path}", ex);
        }

        if (image.Width <= 0 || image.Height <= 0)
            throw new InvalidDataException(
                $"HDR image has invalid dimensions {image.Width}x{image.Height}: {path}");

        if (image.Data is null || image.Data.Length < image.Width * image.Height * 4)
            throw new InvalidDataException($"HDR image has incomplete float pixel data: {path}");

        return new HdrEquirectImage(image.Data, image.Width, image.Height);
    }
}

internal readonly struct HdrEquirectImage(float[] rgba, int width, int height)
{
    public float[] Rgba { get; } = rgba;
    public int Width { get; } = width;
    public int Height { get; } = height;
}
