using Engine.Renderer;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class OrmTexturePackerTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "OrmTexturePackerTests_" + Guid.NewGuid().ToString("N"));

    public OrmTexturePackerTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }
        catch
        {
            // ponytail: best-effort temp cleanup
        }
    }

    [Fact]
    public void PackMaterialMaps_AllNull_ReturnsNull() =>
        OrmTexturePacker.PackMaterialMaps(Path.Combine(_root, "none.bmp"), null, null, null, null)
            .ShouldBeNull();

    [Fact]
    public void PackMaterialMaps_SeparateMetalRoughAo_WritesOrmChannels()
    {
        var metal = WriteSolid(_root, "metal.bmp", 255, 0, 0);
        var rough = WriteSolid(_root, "rough.bmp", 128, 0, 0);
        var ao = WriteSolid(_root, "ao.bmp", 64, 0, 0);
        var dest = Path.Combine(_root, "packed.bmp");

        var result = OrmTexturePacker.PackMaterialMaps(dest, ormPath: null, ao, rough, metal);

        result.ShouldBe(dest);
        var (r, g, b) = ReadTopLeftRgb(dest);
        r.ShouldBe((byte)64);
        g.ShouldBe((byte)128);
        b.ShouldBe((byte)255);
    }

    [Fact]
    public void PackMaterialMaps_SeparateMetalAndRough_ReturnsPath()
    {
        var metal = WriteSolid(_root, "m.bmp", 200, 0, 0);
        var rough = WriteSolid(_root, "r.bmp", 40, 0, 0);
        var dest = Path.Combine(_root, "mat0_orm.bmp");

        var mrPath = OrmTexturePacker.PackMaterialMaps(dest, null, null, rough, metal);

        mrPath.ShouldNotBeNull();
        File.Exists(mrPath).ShouldBeTrue();
        var (_, g, b) = ReadTopLeftRgb(mrPath!);
        g.ShouldBe((byte)40);
        b.ShouldBe((byte)200);
    }

    [Fact]
    public void PackMaterialMaps_OrmWithoutAo_ForcesAoWhite()
    {
        var orm = WriteSolid(_root, "orm.bmp", 10, 80, 200);
        var dest = Path.Combine(_root, "from-orm.bmp");

        OrmTexturePacker.PackMaterialMaps(dest, orm, aoPath: null, null, null);

        var (r, g, b) = ReadTopLeftRgb(dest);
        r.ShouldBe((byte)255);
        g.ShouldBe((byte)80);
        b.ShouldBe((byte)200);
    }

    private static string WriteSolid(string dir, string name, byte r, byte g, byte b)
    {
        var path = Path.Combine(dir, name);
        var rgb = new byte[2 * 2 * 3];
        for (var i = 0; i < 4; i++)
        {
            rgb[i * 3] = r;
            rgb[i * 3 + 1] = g;
            rgb[i * 3 + 2] = b;
        }

        OrmTexturePacker.WriteBgrBmp(path, 2, 2, rgb);
        return path;
    }

    private static (byte R, byte G, byte B) ReadTopLeftRgb(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var offset = BitConverter.ToInt32(bytes, 10);
        var width = BitConverter.ToInt32(bytes, 18);
        var stride = (width * 3 + 3) & ~3;
        // BMP is bottom-up: last row in file is top of image.
        var topRow = offset + stride * (BitConverter.ToInt32(bytes, 22) - 1);
        return (bytes[topRow + 2], bytes[topRow + 1], bytes[topRow]);
    }
}
