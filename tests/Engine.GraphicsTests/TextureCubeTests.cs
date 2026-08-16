using Engine.Platform.OpenGL;
using Shouldly;

namespace Engine.GraphicsTests;

[Trait("Category", "GraphicsIntegration")]
[Collection("GraphicsIntegration")]
public class TextureCubeTests(HeadlessGraphicsContextFixture fixture)
    : IClassFixture<HeadlessGraphicsContextFixture>
{
    [GraphicsFact]
    public void CreateBlack_ProducesBindableCubemap()
    {
        using var cube = OpenGLTextureCube.CreateBlack();

        cube.GetRendererId().ShouldNotBe(0u);
        cube.Bind(3);
        fixture.RendererApi.GetError().ShouldBe(0);
        cube.Unbind();
    }
}
