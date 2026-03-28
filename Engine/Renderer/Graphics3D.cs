using System.Numerics;
using Engine.Renderer.Cameras;
using Engine.Renderer.Shaders;
using Engine.Scene.Components;

namespace Engine.Renderer;

internal sealed class Graphics3D(IRendererAPI rendererApi, IShaderFactory shaderFactory, IMeshFactory meshFactory) : IGraphics3D
{
    private IShader _meshShader = null!;
    private IShader _lightShader = null!;
    private Mesh _cubeMesh = null!;

    private Vector3 _lightPosition = Vector3.Zero;
    private Vector3 _lightColor = Vector3.One;

    private readonly Statistics _stats = new();
    private bool _disposed;

    public void Init()
    {
        _meshShader = shaderFactory.Create("assets/shaders/opengl/lightingShader.vert", "assets/shaders/opengl/lightingShader.frag");
        _lightShader = shaderFactory.Create("assets/shaders/opengl/lightCubeShader.vert", "assets/shaders/opengl/lightCubeShader.frag");
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
        _meshShader.SetMat4("u_ViewProjection", viewProj);
        _meshShader.SetFloat3("u_LightPosition", _lightPosition);
        _meshShader.SetFloat3("u_LightColor", _lightColor);
    }

    public void BeginScene(IViewCamera camera)
    {
        _meshShader.Bind();
        _meshShader.SetMat4("u_ViewProjection", camera.GetViewProjectionMatrix());
        _meshShader.SetFloat3("u_LightPosition", _lightPosition);
        _meshShader.SetFloat3("u_LightColor", _lightColor);
    }

    public void EndScene()
    {
        _meshShader.Unbind();
    }

    public void DrawMesh(Matrix4x4 transform, Mesh mesh, Vector4 color, int entityId = -1)
    {
        _meshShader.Bind();
        _meshShader.SetMat4("u_Model", transform);
        _meshShader.SetFloat3("u_Color", new Vector3(color.X, color.Y, color.Z));
        _meshShader.SetInt("u_EntityID", entityId);
        mesh.Bind();
        rendererApi.DrawIndexed(mesh.GetVertexArray(), (uint)mesh.GetIndexCount());
        _stats.DrawCalls++;
    }

    public void DrawModel(Matrix4x4 transform, MeshComponent meshComponent, ModelRendererComponent modelRenderer, int entityId = -1)
    {
        var mesh = meshComponent.Mesh;
        if (mesh == null)
            return;

        DrawMesh(transform, mesh, modelRenderer.Color, entityId);
    }

    public void SetLightPosition(Vector3 position)
    {
        _lightPosition = position;
    }

    public void SetLightColor(Vector3 color)
    {
        _lightColor = color;
    }

    public void SetShininess(float shininess) { }

    public void BeginLightVisualization(Camera camera, Matrix4x4 transform)
    {
        if (!Matrix4x4.Invert(transform, out var viewMatrix))
            return;
        var viewProj = viewMatrix * camera.GetProjectionMatrix();
        rendererApi.SetDepthTest(false);
        _lightShader.Bind();
        _lightShader.SetMat4("u_ViewProjection", viewProj);
    }

    public void BeginLightVisualization(IViewCamera camera)
    {
        rendererApi.SetDepthTest(false);
        _lightShader.Bind();
        _lightShader.SetMat4("u_ViewProjection", camera.GetViewProjectionMatrix());
    }

    public void DrawLightVisualization(Vector3 position, float scale = 0.2f)
    {
        var model = Matrix4x4.CreateScale(scale) * Matrix4x4.CreateTranslation(position);
        _lightShader.SetMat4("u_Model", model);
        _cubeMesh.Bind();
        rendererApi.DrawIndexed(_cubeMesh.GetVertexArray(), (uint)_cubeMesh.GetIndexCount());
        _stats.DrawCalls++;
    }

    public void EndLightVisualization()
    {
        _lightShader.Unbind();
        rendererApi.SetDepthTest(true);
    }

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

        _meshShader = null!;
        _lightShader = null!;
        _cubeMesh?.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
