using System.Numerics;
using Engine.Core;
using Engine.Scene.Cameras;
using Engine.Renderer.Shaders;
using Engine.Renderer.Textures;

namespace Engine.Renderer;

internal sealed class Graphics3D(
    IRendererAPI rendererApi,
    IShaderFactory shaderFactory,
    IMeshFactory meshFactory,
    ITextureFactory textureFactory) : IGraphics3D
{
    private const string ViewProjectionUniform = "u_ViewProjection";
    private IShader _cubeShader = null!;
    private IShader _texturedShader = null!;
    private IShader? _wireframeShader;
    private Mesh _cubeMesh = null!;

    private Vector3 _ambientColor = Vector3.One;
    private float _ambientStrength = 0.1f;
    private Vector3 _lightDirection = new(0, -1, 0);
    private Vector3 _lightColor = Vector3.Zero;

    private Matrix4x4 _viewProjection = Matrix4x4.Identity;
    private bool _wireframe;
    private bool _wireframeLoadFailed;
    private readonly Statistics _stats = new();
    private bool _disposed;

    public void Init()
    {
        _cubeShader = shaderFactory.Create(
            PathBuilder.Resolve("assets/shaders/OpenGL/flatColorShader.vert"),
            PathBuilder.Resolve("assets/shaders/OpenGL/flatColorShader.frag"));
        _texturedShader = shaderFactory.Create(
            PathBuilder.Resolve("assets/shaders/OpenGL/lightingShader.vert"),
            PathBuilder.Resolve("assets/shaders/OpenGL/lightingShader.frag"));
        _cubeMesh = meshFactory.CreateCube();

        _texturedShader.Bind();
        _texturedShader.SetInt("u_AlbedoMap", 0);
        _texturedShader.SetInt("u_MetallicRoughnessMap", 1);
        _texturedShader.SetInt("u_NormalMap", 2);
        _texturedShader.Unbind();
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

        ApplyCamera(viewMatrix * camera.GetProjectionMatrix(), new Vector3(transform.M41, transform.M42, transform.M43));
    }

    public void BeginScene(IViewCamera camera) =>
        ApplyCamera(camera.GetViewProjectionMatrix(), camera.GetPosition());

    public void EndScene()
    {
    }

    public void SetWireframe(bool enabled)
    {
        if (enabled)
        {
            if (EnsureWireframeShader())
                _wireframe = true;
            return;
        }

        _wireframe = false;
        rendererApi.SetPolygonMode(PolygonMode.Fill);
    }

    public void DrawCube(Matrix4x4 transform, Vector4 color, int entityId = -1)
    {
        rendererApi.SetDepthTest(true);

        if (_wireframe)
        {
            DrawWireframe(_cubeMesh, transform, entityId);
            return;
        }

        BindCommon(_cubeShader, transform, color, entityId);

        _cubeMesh.Bind();
        rendererApi.DrawIndexed(_cubeMesh.GetVertexArray(), (uint)_cubeMesh.GetIndexCount());
        _stats.DrawCalls++;
        _cubeShader.Unbind();
    }

    public void DrawMesh(Matrix4x4 transform, Mesh mesh, MeshMaterial material, Vector4 tint, float metallic, float roughness, int entityId = -1)
    {
        rendererApi.SetDepthTest(true);

        if (_wireframe)
        {
            DrawWireframe(mesh, transform, entityId);
            return;
        }

        BindCommon(_texturedShader, transform, tint, entityId);
        _texturedShader.SetFloat("u_Metallic", metallic);
        _texturedShader.SetFloat("u_Roughness", roughness);
        _texturedShader.SetInt("u_HasAlbedoMap", material.HasAlbedoMap ? 1 : 0);
        _texturedShader.SetInt("u_HasMetallicRoughnessMap", material.HasMetallicRoughnessMap ? 1 : 0);
        _texturedShader.SetInt("u_HasNormalMap", material.HasNormalMap ? 1 : 0);

        (material.AlbedoTexture ?? textureFactory.GetWhiteTexture()).Bind(0);
        (material.MetallicRoughnessTexture ?? textureFactory.GetWhiteTexture()).Bind(1);
        (material.NormalTexture ?? textureFactory.GetFlatNormalTexture()).Bind(2);

        mesh.Bind();
        rendererApi.DrawIndexed(mesh.GetVertexArray(), (uint)mesh.GetIndexCount());
        _stats.DrawCalls++;
        _texturedShader.Unbind();
    }

    public void SetAmbientLight(Vector3 color, float strength)
    {
        _ambientColor = color;
        _ambientStrength = strength;
    }

    public void SetDirectionalLight(Vector3 direction, Vector3 color)
    {
        _lightDirection = direction;
        _lightColor = color;
    }

    private void ApplyCamera(Matrix4x4 viewProjection, Vector3 viewPosition)
    {
        _viewProjection = viewProjection;

        _cubeShader.Bind();
        _cubeShader.SetMat4(ViewProjectionUniform, viewProjection);

        _texturedShader.Bind();
        _texturedShader.SetMat4(ViewProjectionUniform, viewProjection);
        _texturedShader.SetFloat3("u_ViewPosition", viewPosition);

        if (_wireframeShader is not null)
        {
            _wireframeShader.Bind();
            _wireframeShader.SetMat4(ViewProjectionUniform, viewProjection);
        }
    }

    private void DrawWireframe(Mesh mesh, Matrix4x4 transform, int entityId)
    {
        _wireframeShader!.Bind();
        _wireframeShader.SetMat4(ViewProjectionUniform, _viewProjection);
        _wireframeShader.SetMat4("u_Model", transform);
        _wireframeShader.SetFloat4("u_Color", RenderingConstants.WireframeEdgeColor);
        _wireframeShader.SetInt("u_EntityID", entityId);
        mesh.Bind();
        try
        {
            rendererApi.SetPolygonMode(PolygonMode.Line);
            rendererApi.DrawIndexed(mesh.GetVertexArray(), (uint)mesh.GetIndexCount());
            _stats.DrawCalls++;
        }
        finally
        {
            rendererApi.SetPolygonMode(PolygonMode.Fill);
            _wireframeShader.Unbind();
        }
    }

    private bool EnsureWireframeShader()
    {
        if (_wireframeShader is not null)
            return true;
        if (_wireframeLoadFailed)
            return false;

        try
        {
            // ponytail: host BaseDirectory — project AssetsPath has no Editor shaders; latch until Dispose
            _wireframeShader = shaderFactory.Create(
                ResolveHostShader("wireframeShader.vert"),
                ResolveHostShader("wireframeShader.frag"));
            _wireframeShader.Bind();
            _wireframeShader.SetMat4(ViewProjectionUniform, _viewProjection);
            _wireframeShader.Unbind();
            return true;
        }
        catch (Exception ex)
        {
            Serilog.Log.ForContext<Graphics3D>().Error(ex, "Failed to load wireframe shader; falling back to Normal");
            _wireframeShader = null;
            _wireframe = false;
            _wireframeLoadFailed = true;
            rendererApi.SetPolygonMode(PolygonMode.Fill);
            return false;
        }
    }

    private static string ResolveHostShader(string fileName) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "assets", "shaders", "OpenGL", fileName));

    private void BindCommon(IShader shader, Matrix4x4 transform, Vector4 color, int entityId)
    {
        shader.Bind();
        shader.SetMat4("u_Model", transform);
        shader.SetMat4("u_NormalMatrix", ComputeNormalMatrix(transform));
        shader.SetFloat4("u_Color", color);
        shader.SetInt("u_EntityID", entityId);
        shader.SetFloat3("lightColor", _ambientColor);
        shader.SetFloat("strength", _ambientStrength);
        shader.SetFloat3("u_LightDirection", _lightDirection);
        shader.SetFloat3("u_LightColor", _lightColor);
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

        _cubeShader?.Dispose();
        _cubeShader = null!;
        _texturedShader?.Dispose();
        _texturedShader = null!;
        _wireframeShader?.Dispose();
        _wireframeShader = null;
        _cubeMesh?.Dispose();
        _cubeMesh = null!;

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
