using System.Numerics;
using Engine.Core;
using Engine.Scene.Cameras;
using Engine.Renderer.Shaders;

namespace Engine.Renderer;

internal sealed class Graphics3D(
    IRendererAPI rendererApi,
    IShaderFactory shaderFactory,
    IMeshFactory meshFactory) : IGraphics3D
{
    private const string ViewProjectionUniform = "u_ViewProjection";
    private IShader _meshShader = null!;
    private Mesh _cubeMesh = null!;

    private readonly Statistics _stats = new();
    private bool _disposed;

    public void Init()
    {
        _meshShader = shaderFactory.Create(
            PathBuilder.Resolve("assets/shaders/OpenGL/flatColorShader.vert"),
            PathBuilder.Resolve("assets/shaders/OpenGL/flatColorShader.frag"));
        _cubeMesh = meshFactory.CreateCube();
    }

    public void BeginScene(Camera camera, Matrix4x4 transform)
    {
        if (!Matrix4x4.Invert(transform, out var viewMatrix))
        {
            Serilog.Log.ForContext<Graphics3D>().Error(
                "Failed to invert camera transform matrix (M11={M11}, M22={M22}, M33={M33}, M44={M44}). Skipping scene.",
                transform.M11, transform.M22, transform.M33, transform.M44);
            return;
        }

        var viewProj = viewMatrix * camera.GetProjectionMatrix();
        _meshShader.Bind();
        _meshShader.SetMat4(ViewProjectionUniform, viewProj);
    }

    public void BeginScene(IViewCamera camera)
    {
        _meshShader.Bind();
        _meshShader.SetMat4(ViewProjectionUniform, camera.GetViewProjectionMatrix());
    }

    public void EndScene()
    {
        _meshShader.Unbind();
    }

    public void DrawCube(Matrix4x4 transform, Vector4 color, int entityId = -1)
    {
        _meshShader.Bind();
        _meshShader.SetMat4("u_Model", transform);
        _meshShader.SetMat4("u_NormalMatrix", ComputeNormalMatrix(transform));
        _meshShader.SetFloat4("u_Color", color);
        _meshShader.SetInt("u_EntityID", entityId);

        _cubeMesh.Bind();
        rendererApi.DrawIndexed(_cubeMesh.GetVertexArray(), (uint)_cubeMesh.GetIndexCount());
        _stats.DrawCalls++;
    }

    public void SetAmbientLight(Vector3 color, float strength)
    {
        _meshShader.Bind();
        _meshShader.SetFloat3("lightColor", color);
        _meshShader.SetFloat("strength", strength);
    }

    public void SetDirectionalLight(Vector3 direction, Vector3 color)
    {
        _meshShader.Bind();
        _meshShader.SetFloat3("u_LightDirection", direction);
        _meshShader.SetFloat3("u_LightColor", color);
    }

    private static Matrix4x4 ComputeNormalMatrix(Matrix4x4 model) =>
        Matrix4x4.Invert(model, out var inv) ? Matrix4x4.Transpose(inv) : Matrix4x4.Identity;

    public void ResetStats()
    {
        _stats.DrawCalls = 0;
    }

    public Statistics GetStats() => _stats;

    public void SetClearColor(Vector4 color) => rendererApi.SetClearColor(color);

    public void Clear() => rendererApi.Clear();

    public void Dispose()
    {
        if (_disposed)
            return;

        _meshShader?.Dispose();
        _meshShader = null!;
        _cubeMesh?.Dispose();
        _cubeMesh = null!;

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
