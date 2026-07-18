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
    private Mesh _cubeMesh = null!;

    private Matrix4x4 _viewProjection = Matrix4x4.Identity;
    private Vector3 _viewPosition;
    private Vector3 _ambientColor = Vector3.One;
    private float _ambientStrength = 0.1f;
    private Vector3 _lightDirection = new(0, -1, 0);
    private Vector3 _lightColor = Vector3.Zero;

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
        _texturedShader.SetInt("u_SpecularMap", 3);
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

        _viewProjection = viewMatrix * camera.GetProjectionMatrix();
        _viewPosition = new Vector3(transform.M41, transform.M42, transform.M43);
    }

    public void BeginScene(IViewCamera camera)
    {
        _viewProjection = camera.GetViewProjectionMatrix();
        _viewPosition = camera.GetPosition();
    }

    public void EndScene()
    {
    }

    public void DrawCube(Matrix4x4 transform, Vector4 color, int entityId = -1)
    {
        rendererApi.SetDepthTest(true);
        BindCommon(_cubeShader, transform, color, entityId);

        _cubeMesh.Bind();
        rendererApi.DrawIndexed(_cubeMesh.GetVertexArray(), (uint)_cubeMesh.GetIndexCount());
        _stats.DrawCalls++;
        _cubeShader.Unbind();
    }

    public void DrawMesh(Matrix4x4 transform, Mesh mesh, MeshMaterial material, Vector4 tint, float metallic, float roughness, int entityId = -1)
    {
        var meshTransform = mesh.NodeTransform * transform;

        rendererApi.SetDepthTest(true);
        BindCommon(_texturedShader, meshTransform, tint, entityId);
        _texturedShader.SetFloat3("u_ViewPosition", _viewPosition);
        _texturedShader.SetFloat("u_Metallic", System.Math.Clamp(metallic, 0f, 1f));
        _texturedShader.SetFloat("u_Roughness", System.Math.Clamp(roughness, 0f, 1f));
        _texturedShader.SetInt("u_HasAlbedoMap", material.HasAlbedoMap ? 1 : 0);
        _texturedShader.SetInt("u_HasMetallicRoughnessMap", material.HasMetallicRoughnessMap ? 1 : 0);
        _texturedShader.SetInt("u_HasNormalMap", material.HasNormalMap ? 1 : 0);
        _texturedShader.SetInt("u_HasSpecularMap", material.HasSpecularMap ? 1 : 0);

        (material.AlbedoTexture ?? textureFactory.GetWhiteTexture()).Bind(0);
        (material.MetallicRoughnessTexture ?? textureFactory.GetWhiteTexture()).Bind(1);
        (material.NormalTexture ?? textureFactory.GetFlatNormalTexture()).Bind(2);
        (material.SpecularTexture ?? textureFactory.GetWhiteTexture()).Bind(3);

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

    private void BindCommon(IShader shader, Matrix4x4 transform, Vector4 color, int entityId)
    {
        shader.Bind();
        shader.SetMat4(ViewProjectionUniform, _viewProjection);
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
        _cubeMesh?.Dispose();
        _cubeMesh = null!;

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
