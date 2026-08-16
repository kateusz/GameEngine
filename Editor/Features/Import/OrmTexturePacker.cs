using StbImageSharp;

namespace Editor.Features.Import;

/// <summary>
/// Packs AO / roughness / metallic into one glTF ORM texture (R=AO, G=rough, B=metal).
/// Missing channels become 255 so the shader factor still wins (<c>roughness *= mr.g</c>).
/// </summary>
internal static class OrmTexturePacker
{
    /// <summary>
    /// Writes <paramref name="destPath"/> and returns it, or null when there is nothing to pack.
    /// When <paramref name="ormPath"/> is set, G/B come from that texture; R is AO or 255.
    /// Otherwise each map is read as grayscale from the R channel.
    /// </summary>
    public static string? PackMaterialMaps(
        string destPath,
        string? ormPath,
        string? aoPath,
        string? roughnessPath,
        string? metallicPath)
    {
        if (ormPath == null && aoPath == null && roughnessPath == null && metallicPath == null)
            return null;

        ArgumentException.ThrowIfNullOrWhiteSpace(destPath);

        var ao = LoadOptional(aoPath);
        var orm = LoadOptional(ormPath);
        var rough = orm == null ? LoadOptional(roughnessPath) : null;
        var metal = orm == null ? LoadOptional(metallicPath) : null;

        if (ao == null && orm == null && rough == null && metal == null)
            return null;

        var width = 1;
        var height = 1;
        Grow(ref width, ref height, ao);
        Grow(ref width, ref height, orm);
        Grow(ref width, ref height, rough);
        Grow(ref width, ref height, metal);

        var rgb = new byte[width * height * 3];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var i = (y * width + x) * 3;
                rgb[i] = ao != null ? Sample(ao, x, y, width, height, 0) : (byte)255;
                if (orm != null)
                {
                    rgb[i + 1] = Sample(orm, x, y, width, height, 1);
                    rgb[i + 2] = Sample(orm, x, y, width, height, 2);
                }
                else
                {
                    rgb[i + 1] = rough != null ? Sample(rough, x, y, width, height, 0) : (byte)255;
                    rgb[i + 2] = metal != null ? Sample(metal, x, y, width, height, 0) : (byte)255;
                }
            }
        }

        var destDir = Path.GetDirectoryName(destPath);
        if (!string.IsNullOrEmpty(destDir))
            Directory.CreateDirectory(destDir);

        WriteBgrBmp(destPath, width, height, rgb);
        return destPath;
    }

    private static void Grow(ref int width, ref int height, ImageResult? image)
    {
        if (image == null)
            return;
        width = System.Math.Max(width, image.Width);
        height = System.Math.Max(height, image.Height);
    }

    private static ImageResult? LoadOptional(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;

        // File-order pixels; WriteBgrBmp stores a standard bottom-up BMP.
        // GPU load flips via stbi — same path as every other albedo/MR texture.
        StbImage.stbi_set_flip_vertically_on_load(0);
        try
        {
            using var stream = File.OpenRead(path);
            return ImageResult.FromStream(stream, ColorComponents.RedGreenBlue);
        }
        finally
        {
            StbImage.stbi_set_flip_vertically_on_load(1);
        }
    }

    private static byte Sample(ImageResult image, int x, int y, int destW, int destH, int channel)
    {
        var sx = (int)((x + 0.5f) * image.Width / destW);
        var sy = (int)((y + 0.5f) * image.Height / destH);
        sx = System.Math.Clamp(sx, 0, image.Width - 1);
        sy = System.Math.Clamp(sy, 0, image.Height - 1);
        return image.Data[(sy * image.Width + sx) * 3 + channel];
    }

    internal static void WriteBgrBmp(string path, int width, int height, byte[] rgbTopDown)
    {
        var stride = (width * 3 + 3) & ~3;
        var pixelBytes = stride * height;
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);
        writer.Write((ushort)0x4D42);
        writer.Write(14 + 40 + pixelBytes);
        writer.Write(0);
        writer.Write(14 + 40);
        writer.Write(40);
        writer.Write(width);
        writer.Write(height);
        writer.Write((ushort)1);
        writer.Write((ushort)24);
        writer.Write(0);
        writer.Write(pixelBytes);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);

        var row = new byte[stride];
        for (var y = height - 1; y >= 0; y--)
        {
            Array.Clear(row);
            for (var x = 0; x < width; x++)
            {
                var i = (y * width + x) * 3;
                row[x * 3] = rgbTopDown[i + 2];
                row[x * 3 + 1] = rgbTopDown[i + 1];
                row[x * 3 + 2] = rgbTopDown[i];
            }

            writer.Write(row);
        }
    }
}
