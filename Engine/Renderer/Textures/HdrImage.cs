using StbImageSharp;

namespace Engine.Renderer.Textures;

internal readonly record struct HdrImageData(float[] Pixels, int Width, int Height);

internal static class HdrImage
{
    /// <summary>
    /// Decodes a radiance .hdr into linear RGB floats. Flips vertically to match the
    /// equirect UV convention used by equirectToCube.frag; if a loaded sky ever appears
    /// upside down, this flip is the calibration knob.
    /// </summary>
    public static HdrImageData Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"HDR file not found: {path}", path);

        StbImage.stbi_set_flip_vertically_on_load(1);
        ImageResultFloat image;
        using (var stream = File.OpenRead(path))
            image = ImageResultFloat.FromStream(stream, ColorComponents.RedGreenBlue);

        return new HdrImageData(image.Data, image.Width, image.Height);
    }
}
