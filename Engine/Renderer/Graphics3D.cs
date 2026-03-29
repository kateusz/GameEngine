using System.Numerics;
using Engine.Renderer.Cameras;
using Engine.Renderer.Shaders;
using Engine.Renderer.Textures;
using Engine.Scene.Components;

namespace Engine.Renderer;

internal sealed class Graphics3D(
    IRendererAPI rendererApi,
    IShaderFactory shaderFactory,
    IMeshFactory meshFactory,
    ITextureFactory textureFactory) : IGraphics3D
{
    private IShader _meshShader = null!;
    private IShader _lightShader = null!;
    private Mesh _cubeMesh = null!;

    private Vector3 _lightPosition = Vector3.Zero;
    private Vector3 _lightDirection = -Vector3.UnitY;
    private int _lightType = 0;
    private Vector3 _lightColor = Vector3.One;

    private readonly Statistics _stats = new();
    private bool _disposed;

    public void Init()
    {
        _meshShader = shaderFactory.Create("assets/shaders/opengl/lightingShader.vert",
            "assets/shaders/opengl/lightingShader.frag");
        _lightShader = shaderFactory.Create("assets/shaders/opengl/lightCubeShader.vert",
            "assets/shaders/opengl/lightCubeShader.frag");
        _cubeMesh = meshFactory.CreateCube();

        _meshShader.Bind();
        _meshShader.SetInt("u_DiffuseMap", 0);
        _meshShader.SetInt("u_SpecularMap", 1);
        _meshShader.SetInt("u_NormalMap", 2);
        _meshShader.Unbind();
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
        var cameraPos = new Vector3(transform.M41, transform.M42, transform.M43);
        _meshShader.Bind();
        _meshShader.SetMat4("u_ViewProjection", viewProj);
        _meshShader.SetFloat3("u_LightPosition", _lightPosition);
        _meshShader.SetFloat3("u_LightDirection", _lightDirection);
        _meshShader.SetInt("u_LightType", _lightType);
        _meshShader.SetFloat3("u_LightColor", _lightColor);
        _meshShader.SetFloat3("u_ViewPosition", cameraPos);
    }

    public void BeginScene(IViewCamera camera)
    {
        _meshShader.Bind();
        _meshShader.SetMat4("u_ViewProjection", camera.GetViewProjectionMatrix());
        _meshShader.SetFloat3("u_LightPosition", _lightPosition);
        _meshShader.SetFloat3("u_LightDirection", _lightDirection);
        _meshShader.SetInt("u_LightType", _lightType);
        _meshShader.SetFloat3("u_LightColor", _lightColor);
        _meshShader.SetFloat3("u_ViewPosition", camera.GetPosition());
    }

    public void EndScene()
    {
        _meshShader.Unbind();
    }

    public void DrawMesh(Matrix4x4 transform, Mesh mesh, MeshMaterial material, int entityId = -1)
    {
        Matrix4x4.Invert(transform, out var invTransform);
        var normalMatrix = Matrix4x4.Transpose(invTransform);

        _meshShader.Bind();
        _meshShader.SetMat4("u_Model", transform);
        _meshShader.SetMat4("u_NormalMatrix", normalMatrix);
        _meshShader.SetFloat4("u_Color", Vector4.One);
        _meshShader.SetInt("u_EntityID", entityId);

        _meshShader.SetFloat("u_Shininess", material.Shininess);
        _meshShader.SetInt("u_HasDiffuseMap", material.HasDiffuseMap ? 1 : 0);
        _meshShader.SetInt("u_HasSpecularMap", material.HasSpecularMap ? 1 : 0);
        _meshShader.SetInt("u_HasNormalMap", material.HasNormalMap ? 1 : 0);

        var diffuse = material.DiffuseTexture ?? textureFactory.GetWhiteTexture();
        var specular = material.SpecularTexture ?? textureFactory.GetBlackTexture();
        var normal = material.NormalTexture ?? textureFactory.GetFlatNormalTexture();

        diffuse.Bind(0);
        specular.Bind(1);
        normal.Bind(2);

        mesh.Bind();
        rendererApi.DrawIndexed(mesh.GetVertexArray(), (uint)mesh.GetIndexCount());
        _stats.DrawCalls++;
    }

    public void DrawModel(Matrix4x4 transform, MeshComponent meshComponent, ModelRendererComponent modelRenderer,
        int entityId = -1)
    {
        if (meshComponent.Meshes.Count == 0)
            return;

        _meshShader.Bind();
        _meshShader.SetFloat4("u_Color", modelRenderer.Color);
        _meshShader.SetInt("u_EntityID", entityId);

        for (var i = 0; i < meshComponent.Meshes.Count; i++)
        {
            var mesh = meshComponent.Meshes[i];
            var meshTransform = mesh.NodeTransform * transform;

            Matrix4x4.Invert(meshTransform, out var invTransform);
            var normalMatrix = Matrix4x4.Transpose(invTransform);

            _meshShader.SetMat4("u_Model", meshTransform);
            _meshShader.SetMat4("u_NormalMatrix", normalMatrix);

            var material = i < modelRenderer.Materials.Count ? modelRenderer.Materials[i] : new MeshMaterial();

            _meshShader.SetFloat("u_Shininess", material.Shininess);
            _meshShader.SetInt("u_HasDiffuseMap", material.HasDiffuseMap ? 1 : 0);
            _meshShader.SetInt("u_HasSpecularMap", material.HasSpecularMap ? 1 : 0);
            _meshShader.SetInt("u_HasNormalMap", material.HasNormalMap ? 1 : 0);

            var diffuse = material.DiffuseTexture ?? textureFactory.GetWhiteTexture();
            var specular = material.SpecularTexture ?? textureFactory.GetBlackTexture();
            var normalTex = material.NormalTexture ?? textureFactory.GetFlatNormalTexture();

            diffuse.Bind(0);
            specular.Bind(1);
            normalTex.Bind(2);

            mesh.Bind();
            rendererApi.DrawIndexed(mesh.GetVertexArray(), (uint)mesh.GetIndexCount());
            _stats.DrawCalls++;
        }
    }

    public void SetLightPosition(Vector3 position) => _lightPosition = position;
    public void SetLightDirection(Vector3 direction) => _lightDirection = direction;
    public void SetLightType(int type) => _lightType = type;
    public void SetLightColor(Vector3 color) => _lightColor = color;

    public void SetShininess(float shininess)
    {
    }

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

        _meshShader?.Dispose();
        _meshShader = null!;
        _lightShader?.Dispose();
        _lightShader = null!;
        _cubeMesh?.Dispose();
        _cubeMesh = null!;

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}