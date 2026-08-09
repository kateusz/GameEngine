using System.Numerics;
using Engine.Platform.OpenGL;
using Engine.Platform.SilkNet;
using Shouldly;
using Silk.NET.OpenGL;
using Xunit.Abstractions;

namespace Engine.GraphicsTests;

/// <summary>
/// The REAL production lighting shader against the live GL driver:
/// (1) it must link at all — u_BoneMatrices[100] is 1600 uniform components; we keep MaxBones=100
///     (Mixamo often needs &gt;64) and require GL_MAX_VERTEX_UNIFORM_COMPONENTS ≥ 1600 (OpenGLShader
///     fails fast when the driver is below that; GL 3.3 only guarantees 1024);
/// (2) a full 100-matrix palette upload must land where the driver says each array element
///     lives — element location stride differs per driver (Apple GL: 4 per mat4, most
///     desktop drivers: 1), which is exactly the kind of macOS/Windows divergence that
///     renders fine on one OS and explodes triangles on the other.
/// </summary>
[Trait("Category", "GraphicsIntegration")]
[Collection("GraphicsIntegration")]
public class LightingShaderBonePaletteTests : IClassFixture<HeadlessGraphicsContextFixture>
{
    private const int BoneCount = 100;
    private const int RequiredVertexUniformComponents = BoneCount * 16;
    private readonly ITestOutputHelper _output;

    public LightingShaderBonePaletteTests(HeadlessGraphicsContextFixture _, ITestOutputHelper output)
    {
        _output = output;
    }

    [GraphicsFact]
    public void RealLightingShader_Links_AndFullPaletteUploadLandsOnDriverElementLocations()
    {
        var repoRoot = FindRepoRoot();
        repoRoot.ShouldNotBeNull("could not locate repo root from test base directory");
        var vert = Path.Combine(repoRoot, "Editor", "assets", "shaders", "OpenGL", "lightingShader.vert");
        var frag = Path.Combine(repoRoot, "Editor", "assets", "shaders", "OpenGL", "lightingShader.frag");
        File.Exists(vert).ShouldBeTrue(vert);
        File.Exists(frag).ShouldBeTrue(frag);

        SilkNetContext.GL.GetInteger(GetPName.MaxVertexUniformComponents, out int maxComponents);
        _output.WriteLine($"GL_MAX_VERTEX_UNIFORM_COMPONENTS = {maxComponents} (palette needs {RequiredVertexUniformComponents})");
        maxComponents.ShouldBeGreaterThanOrEqualTo(RequiredVertexUniformComponents,
            "driver must meet the explicit bone-palette capability gate (MaxBones=100 → 1600 components)");

        // Constructor throws on compile/link failure or insufficient uniform budget.
        using var shader = new OpenGLShader(vert, frag);
        shader.Bind();

        DrainGlErrors();

        var palette = new Matrix4x4[BoneCount];
        for (var i = 0; i < BoneCount; i++)
            palette[i] = Matrix4x4.CreateTranslation(i + 1f, 2f * i, 3f * i) with { M14 = 0.25f * i };
        shader.SetMat4Array("u_BoneMatrices[0]", palette, BoneCount);

        SilkNetContext.GL.GetError().ShouldBe(GLEnum.NoError,
            "palette upload raised a GL error — an element upload hit an invalid uniform location");

        SilkNetContext.GL.GetInteger(GetPName.CurrentProgram, out int program);
        var loc0 = SilkNetContext.GL.GetUniformLocation((uint)program, "u_BoneMatrices[0]");
        var loc1 = SilkNetContext.GL.GetUniformLocation((uint)program, "u_BoneMatrices[1]");
        _output.WriteLine($"driver element stride = {loc1 - loc0} (Apple GL: 4, conformant desktop drivers: 1)");

        foreach (var i in new[] { 0, 1, 31, 50, 99 })
        {
            var actual = ReadUniformMat4((uint)program, $"u_BoneMatrices[{i}]");
            var expected = UploadViaSetMat4AndRead(shader, (uint)program, palette[i]);
            for (var c = 0; c < 16; c++)
                actual[c].ShouldBe(expected[c], 1e-5f, $"element {i}, component {c}");
        }
    }

    private static void DrainGlErrors()
    {
        while (SilkNetContext.GL.GetError() != GLEnum.NoError)
        {
        }
    }

    /// <summary>Ground truth: same matrix through the proven single-mat4 path (u_Model).</summary>
    private static float[] UploadViaSetMat4AndRead(OpenGLShader shader, uint program, Matrix4x4 matrix)
    {
        shader.SetMat4("u_Model", matrix);
        return ReadUniformMat4(program, "u_Model");
    }

    private static float[] ReadUniformMat4(uint program, string name)
    {
        var location = SilkNetContext.GL.GetUniformLocation(program, name);
        location.ShouldBeGreaterThanOrEqualTo(0, name);

        var values = new float[16];
        unsafe
        {
            fixed (float* ptr = values)
                SilkNetContext.GL.GetUniform(program, location, ptr);
        }

        return values;
    }

    private static string? FindRepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Editor", "assets", "shaders", "OpenGL", "lightingShader.vert"))
                && File.Exists(Path.Combine(dir.FullName, "Engine", "Engine.csproj")))
                return dir.FullName;
        }

        return null;
    }
}
