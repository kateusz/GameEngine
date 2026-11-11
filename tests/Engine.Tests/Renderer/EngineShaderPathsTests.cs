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
    [InlineData(ShaderId.Depth, "depth")]
    [InlineData(ShaderId.Fxaa, "fxaa")]
    [InlineData(ShaderId.BrdfLut, "brdfLut")]
    public void Resolve_UsesHostOutputOpenGLTree(ShaderId shader, string name)
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "assets", "shaders", "OpenGL");

        var (vert, frag) = EngineShaderPaths.Resolve(shader);

        vert.ShouldBe(Path.Combine(dir, name + ".vert"));
        frag.ShouldBe(Path.Combine(dir, name + ".frag"));
    }

    [Fact]
    public void Resolve_SelectionOutline_ReusesFxaaVertexShader()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "assets", "shaders", "OpenGL");
        var (vert, frag) = EngineShaderPaths.Resolve(ShaderId.SelectionOutline);
        vert.ShouldBe(Path.Combine(dir, "fxaa.vert"));
        frag.ShouldBe(Path.Combine(dir, "selectionOutline.frag"));
    }

    [Fact]
    public void Resolve_Tonemap_ReusesFxaaVertexShader()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "assets", "shaders", "OpenGL");
        var (vert, frag) = EngineShaderPaths.Resolve(ShaderId.Tonemap);
        vert.ShouldBe(Path.Combine(dir, "fxaa.vert"));
        frag.ShouldBe(Path.Combine(dir, "tonemap.frag"));
    }

    [Theory]
    [InlineData(ShaderId.BloomExtract, "bloomExtract.frag")]
    [InlineData(ShaderId.BloomBlur, "bloomBlur.frag")]
    public void Resolve_Bloom_ReusesFxaaVertexShader(ShaderId shader, string fragment)
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "assets", "shaders", "OpenGL");
        var (vert, frag) = EngineShaderPaths.Resolve(shader);
        vert.ShouldBe(Path.Combine(dir, "fxaa.vert"));
        frag.ShouldBe(Path.Combine(dir, fragment));
    }

    [Fact]
    public void Resolve_Emissive_ReusesModelVertexShader()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "assets", "shaders", "OpenGL");
        var (vert, frag) = EngineShaderPaths.Resolve(ShaderId.Emissive);
        vert.ShouldBe(Path.Combine(dir, "modelShader.vert"));
        frag.ShouldBe(Path.Combine(dir, "emissive.frag"));
        File.ReadAllText(vert).ShouldContain("frame.glsl");
    }

    [Fact]
    public void OpenGlShaders_AreGlsl410_AndDropGpuShader5()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "assets", "shaders", "OpenGL");
        Directory.Exists(dir).ShouldBeTrue();
        foreach (var file in Directory.GetFiles(dir, "*.*"))
        {
            var ext = Path.GetExtension(file);
            if (ext is not (".vert" or ".frag"))
                continue;
            var first = File.ReadLines(file).First();
            first.ShouldBe("#version 410 core", file);
        }

        File.ReadAllText(Path.Combine(dir, "cube.frag")).ShouldNotContain("GL_ARB_gpu_shader5");
        File.ReadAllText(Path.Combine(dir, "modelShader.frag")).ShouldNotContain("GL_ARB_gpu_shader5");
    }

    [Theory]
    [InlineData(ShaderId.Irradiance, "irradiance.frag")]
    [InlineData(ShaderId.Prefilter, "prefilter.frag")]
    public void Resolve_IblConvolution_ReusesSkyboxVertexShader(ShaderId shader, string fragment)
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "assets", "shaders", "OpenGL");
        var (vert, frag) = EngineShaderPaths.Resolve(shader);
        vert.ShouldBe(Path.Combine(dir, "skybox.vert"));
        frag.ShouldBe(Path.Combine(dir, fragment));
    }
}
