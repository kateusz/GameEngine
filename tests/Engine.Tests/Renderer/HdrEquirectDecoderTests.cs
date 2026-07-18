using Engine.Renderer.Textures;
using Shouldly;

namespace Engine.Tests.Renderer;

public class HdrEquirectDecoderTests
{
    private static string FixturePath =>
        Path.Combine(AppContext.BaseDirectory, "TestAssets", "Hdr", "tiny.hdr");

    [Fact]
    public void Decode_TinyHdr_ShouldReturnRgbaFloatPixels()
    {
        var image = HdrEquirectDecoder.Decode(FixturePath);

        image.Width.ShouldBe(1);
        image.Height.ShouldBe(1);
        image.Rgba.Length.ShouldBe(4);
        image.Rgba.ShouldAllBe(v => !float.IsNaN(v) && !float.IsInfinity(v));

        image.Rgba[0].ShouldBe(1.5f, 0.01);
        image.Rgba[1].ShouldBe(1.0f, 0.01);
        image.Rgba[2].ShouldBe(0.5f, 0.01);
        image.Rgba[3].ShouldBe(1f);
    }

    [Fact]
    public void Decode_MissingFile_ShouldThrowFileNotFound()
    {
        Should.Throw<FileNotFoundException>(() =>
            HdrEquirectDecoder.Decode(Path.Combine(AppContext.BaseDirectory, "missing.hdr")));
    }

    [Fact]
    public void Decode_CorruptFile_ShouldThrowInvalidData()
    {
        var path = Path.Combine(Path.GetTempPath(), $"corrupt-{Guid.NewGuid():N}.hdr");
        try
        {
            File.WriteAllText(path, "not a radiance hdr");
            Should.Throw<InvalidDataException>(() => HdrEquirectDecoder.Decode(path));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
