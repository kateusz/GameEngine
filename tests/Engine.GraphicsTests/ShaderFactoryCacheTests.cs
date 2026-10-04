using Engine.Platform.OpenGL;
using Shouldly;

namespace Engine.GraphicsTests;

[Trait("Category", "GraphicsIntegration")]
[Collection("GraphicsIntegration")]
public class ShaderFactoryCacheTests(HeadlessGraphicsContextFixture fixture)
    : IClassFixture<HeadlessGraphicsContextFixture>
{
    [GraphicsFact]
    public void Create_SamePaths_ReturnsCachedInstance_DisposeDeletesProgram()
    {
        _ = fixture;
        var vert = Path.Combine(AppContext.BaseDirectory, "assets", "shaders", "OpenGL", "cube.vert");
        var frag = Path.Combine(AppContext.BaseDirectory, "assets", "shaders", "OpenGL", "cube.frag");
        var factory = new ShaderFactory();
        var first = factory.Create(vert, frag);
        var second = factory.Create(vert, frag);
        first.ShouldBeSameAs(second);

        var id = ((OpenGLShader)first).RendererId;
        id.ShouldNotBe(0u);
        GlBufferQueries.IsProgramAlive(id).ShouldBeTrue();

        factory.Dispose();
        GlBufferQueries.IsProgramAlive(id).ShouldBeFalse();
    }

    [GraphicsFact]
    public void Create_InvalidShader_Throws()
    {
        _ = fixture;
        var vert = Path.Combine(Path.GetTempPath(), $"bad-{Guid.NewGuid():N}.vert");
        var frag = Path.Combine(AppContext.BaseDirectory, "assets", "shaders", "OpenGL", "cube.frag");
        File.WriteAllText(vert, "not a shader");
        var factory = new ShaderFactory();
        try
        {
            Should.Throw<InvalidOperationException>(() => factory.Create(vert, frag));
        }
        finally
        {
            factory.Dispose();
            File.Delete(vert);
        }
    }

    [GraphicsFact]
    public void Create_PointDepthShader_Compiles()
    {
        _ = fixture;
        var vert = Path.Combine(AppContext.BaseDirectory, "assets", "shaders", "OpenGL", "pointDepth.vert");
        var frag = Path.Combine(AppContext.BaseDirectory, "assets", "shaders", "OpenGL", "pointDepth.frag");
        var factory = new ShaderFactory();
        var shader = factory.Create(vert, frag);
        var id = ((OpenGLShader)shader).RendererId;
        id.ShouldNotBe(0u);
        GlBufferQueries.IsProgramAlive(id).ShouldBeTrue();
        factory.Dispose();
    }

    [GraphicsFact]
    public void Create_IblShaders_Compile()
    {
        _ = fixture;
        var dir = Path.Combine(AppContext.BaseDirectory, "assets", "shaders", "OpenGL");
        var factory = new ShaderFactory();
        try
        {
            foreach (var (vertName, fragName) in new[]
            {
                ("skybox.vert", "irradiance.frag"),
                ("skybox.vert", "prefilter.frag"),
                ("brdfLut.vert", "brdfLut.frag")
            })
            {
                var shader = factory.Create(Path.Combine(dir, vertName), Path.Combine(dir, fragName));
                var id = ((OpenGLShader)shader).RendererId;
                id.ShouldNotBe(0u);
                GlBufferQueries.IsProgramAlive(id).ShouldBeTrue();
            }
        }
        finally
        {
            factory.Dispose();
        }
    }
}
