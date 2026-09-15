using Engine.Renderer.Shaders;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class EngineShaderPathsTests
{
    [Theory]
    [InlineData(ShaderId.Texture, "textureShader")]
    [InlineData(ShaderId.Line, "lineShader")]
    [InlineData(ShaderId.Cube, "cube")]
    [InlineData(ShaderId.Model, "modelShader")]
    public void Resolve_UsesHostOutputOpenGLTree(ShaderId shader, string name)
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "assets", "shaders", "OpenGL");

        var (vert, frag) = EngineShaderPaths.Resolve(shader);

        vert.ShouldBe(Path.Combine(dir, name + ".vert"));
        frag.ShouldBe(Path.Combine(dir, name + ".frag"));
    }
}
