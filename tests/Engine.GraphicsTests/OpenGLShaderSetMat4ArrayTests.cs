using System.Numerics;
using Engine.Platform.OpenGL;
using Engine.Platform.SilkNet;
using Shouldly;
using Silk.NET.OpenGL;

namespace Engine.GraphicsTests;

[Trait("Category", "GraphicsIntegration")]
[Collection("GraphicsIntegration")]
public class OpenGLShaderSetMat4ArrayTests : IClassFixture<HeadlessGraphicsContextFixture>, IDisposable
{
    private readonly string _tempDir;
    private readonly string _vertPath;
    private readonly string _fragPath;

    // Required so fixture constructs a GL context before these tests run.
    public OpenGLShaderSetMat4ArrayTests(HeadlessGraphicsContextFixture _)
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "OpenGLShaderSetMat4ArrayTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _vertPath = Path.Combine(_tempDir, "bone.vert");
        _fragPath = Path.Combine(_tempDir, "bone.frag");

        File.WriteAllText(_vertPath, """
            #version 330 core
            layout(location = 0) in vec3 a_Position;
            uniform mat4 u_BoneMatrices[4];
            uniform mat4 u_Single;
            void main()
            {
                gl_Position = vec4(a_Position, 1.0) * u_BoneMatrices[0] * u_Single;
            }
            """);

        File.WriteAllText(_fragPath, """
            #version 330 core
            out vec4 FragColor;
            void main() { FragColor = vec4(1.0); }
            """);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [GraphicsFact]
    public void SetMat4Array_UploadsExactlyCountMatrices()
    {
        using var shader = new OpenGLShader(_vertPath, _fragPath);
        shader.Bind();

        var identity = Matrix4x4.Identity;
        var marker0 = Matrix4x4.CreateTranslation(1f, 2f, 3f);
        var marker1 = Matrix4x4.CreateScale(4f, 5f, 6f);
        var sentinel = Matrix4x4.CreateTranslation(9f, 9f, 9f);

        // Seed all four slots, then overwrite only the first two via count=2.
        shader.SetMat4Array("u_BoneMatrices[0]", [identity, identity, sentinel, sentinel], 4);
        shader.SetMat4Array("u_BoneMatrices[0]", [marker0, marker1, Matrix4x4.Identity, Matrix4x4.Identity], 2);

        AssertMat4Equal(
            ReadUniformMat4(shader, "u_BoneMatrices[0]"),
            UploadViaSetMat4AndRead(shader, marker0));
        AssertMat4Equal(
            ReadUniformMat4(shader, "u_BoneMatrices[1]"),
            UploadViaSetMat4AndRead(shader, marker1));
        AssertMat4Equal(
            ReadUniformMat4(shader, "u_BoneMatrices[2]"),
            UploadViaSetMat4AndRead(shader, sentinel));
        AssertMat4Equal(
            ReadUniformMat4(shader, "u_BoneMatrices[3]"),
            UploadViaSetMat4AndRead(shader, sentinel));
    }

    [GraphicsFact]
    public void SetMat4Array_TransposeMatchesSetMat4()
    {
        using var shader = new OpenGLShader(_vertPath, _fragPath);
        shader.Bind();

        var matrix = new Matrix4x4(
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 10, 11, 12,
            13, 14, 15, 16);

        shader.SetMat4("u_Single", matrix);
        var fromSetMat4 = ReadUniformMat4(shader, "u_Single");

        shader.SetMat4Array("u_BoneMatrices[0]", [matrix], 1);
        var fromSetMat4Array = ReadUniformMat4(shader, "u_BoneMatrices[0]");

        AssertMat4Equal(fromSetMat4Array, fromSetMat4);
    }

    private static float[] UploadViaSetMat4AndRead(OpenGLShader shader, Matrix4x4 matrix)
    {
        shader.SetMat4("u_Single", matrix);
        return ReadUniformMat4(shader, "u_Single");
    }

    private static float[] ReadUniformMat4(OpenGLShader shader, string name)
    {
        shader.Bind();
        SilkNetContext.GL.GetInteger(GetPName.CurrentProgram, out int program);
        var location = SilkNetContext.GL.GetUniformLocation((uint)program, name);
        location.ShouldBeGreaterThanOrEqualTo(0);

        var values = new float[16];
        unsafe
        {
            fixed (float* ptr = values)
                SilkNetContext.GL.GetUniform((uint)program, location, ptr);
        }

        return values;
    }

    private static void AssertMat4Equal(float[] actual, float[] expected)
    {
        actual.Length.ShouldBe(16);
        expected.Length.ShouldBe(16);
        for (var i = 0; i < 16; i++)
            actual[i].ShouldBe(expected[i], 1e-5f);
    }
}
