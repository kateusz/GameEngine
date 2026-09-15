using Engine.Platform.OpenGL;
using Shouldly;

namespace Engine.GraphicsTests;

[Trait("Category", "GraphicsIntegration")]
[Collection("GraphicsIntegration")]
public class TextureFactoryDisposeTests(HeadlessGraphicsContextFixture fixture)
    : IClassFixture<HeadlessGraphicsContextFixture>
{
    [GraphicsFact]
    public void Dispose_DeletesCachedPathTexture_WhileContextIsCurrent()
    {
        _ = fixture;
        var path = WriteTempTga();
        var factory = new TextureFactory();
        try
        {
            var texture = factory.Create(path);
            var id = texture.GetRendererId();
            id.ShouldNotBe(0u);
            GlBufferQueries.IsTextureAlive(id).ShouldBeTrue();

            factory.Dispose();

            GlBufferQueries.IsTextureAlive(id).ShouldBeFalse();
        }
        finally
        {
            factory.Dispose();
            File.Delete(path);
        }
    }

    private static string WriteTempTga()
    {
        var path = Path.Combine(Path.GetTempPath(), $"tex-{Guid.NewGuid():N}.tga");
        var header = new byte[18];
        header[2] = 2;
        header[12] = 1;
        header[14] = 1;
        header[16] = 32;
        header[17] = 8;
        using var stream = File.Create(path);
        stream.Write(header);
        stream.Write([10, 20, 30, 40]);
        return path;
    }
}
