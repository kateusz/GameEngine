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
    private const int MaxPointLights = 16;
    private const string ViewProjectionUniform = "u_ViewProjection";
    private IShader _meshShader = null!;
    private IShader _lightShader = null!;
    private Mesh _cubeMesh = null!;

    private bool _directionalLightEnabled;
    private Vector3 _directionalLightDirection = -Vector3.UnitY;
    private Vector3 _directionalLightColor = Vector3.One;
    private float _directionalLightStrength = 1.0f;

    private bool _ambientLightEnabled;
    private Vector3 _ambientLightColor = Vector3.One;
    private float _ambientLightStrength = 0.1f;

    private readonly PointLightData[] _pointLights = new PointLightData[MaxPointLights];
    private int _pointLightCount;

    private readonly Statistics _stats = new();
    private bool _disposed;

    public void Init()
    {
        _meshShader = shaderFactory.Create("assets/shaders/OpenGL/lightingShader.vert",
            "assets/shaders/OpenGL/pbrShader.frag");
        _lightShader = shaderFactory.Create("assets/shaders/OpenGL/lightCubeShader.vert",
            "assets/shaders/OpenGL/lightCubeShader.frag");
        _cubeMesh = meshFactory.CreateCube();

        _meshShader.Bind();
        _meshShader.SetInt("u_BaseColor", 0);
        _meshShader.SetInt("u_MetallicRoughness", 1);
        _meshShader.SetInt("u_Normal", 2);
        _meshShader.SetInt("u_AO", 3);
        _meshShader.SetInt("u_Emissive", 4);
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
        _meshShader.SetMat4(ViewProjectionUniform, viewProj);
        UploadLightUniforms();
        _meshShader.SetFloat3("u_ViewPosition", cameraPos);
    }

    public void BeginScene(IViewCamera camera)
    {
        _meshShader.Bind();
        _meshShader.SetMat4(ViewProjectionUniform, camera.GetViewProjectionMatrix());
        UploadLightUniforms();
        _meshShader.SetFloat3("u_ViewPosition", camera.GetPosition());
    }

    public void EndScene()
    {
        _meshShader.Unbind();
    }

    public void DrawMesh(Matrix4x4 transform, Mesh mesh, PbrMaterial material, int entityId = -1)
    {
        Matrix4x4.Invert(transform, out var invTransform);
        var normalMatrix = Matrix4x4.Transpose(invTransform);

        _meshShader.Bind();
        _meshShader.SetMat4("u_Model", transform);
        _meshShader.SetMat4("u_NormalMatrix", normalMatrix);
        _meshShader.SetInt("u_EntityID", entityId);

        BindPbrMaterial(material);

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
        _meshShader.SetInt("u_EntityID", entityId);

        for (var i = 0; i < meshComponent.Meshes.Count; i++)
        {
            var mesh = meshComponent.Meshes[i];
            var meshTransform = mesh.NodeTransform * transform;

            Matrix4x4.Invert(meshTransform, out var invTransform);
            var normalMatrix = Matrix4x4.Transpose(invTransform);

            _meshShader.SetMat4("u_Model", meshTransform);
            _meshShader.SetMat4("u_NormalMatrix", normalMatrix);

            var material = i < modelRenderer.Materials.Count ? modelRenderer.Materials[i] : new PbrMaterial();
            BindPbrMaterial(material);

            mesh.Bind();
            rendererApi.DrawIndexed(mesh.GetVertexArray(), (uint)mesh.GetIndexCount());
            _stats.DrawCalls++;
        }
    }

    public void SetDirectionalLight(bool enabled, Vector3 direction, Vector3 color, float strength)
    {
        _directionalLightEnabled = enabled;
        _directionalLightDirection = direction;
        _directionalLightColor = color;
        _directionalLightStrength = strength;
    }

    public void SetAmbientLight(bool enabled, Vector3 color, float strength)
    {
        _ambientLightEnabled = enabled;
        _ambientLightColor = color;
        _ambientLightStrength = strength;
    }

    public void SetPointLights(IReadOnlyList<PointLightData> pointLights)
    {
        var count = System.Math.Min(pointLights.Count, MaxPointLights);
        _pointLightCount = count;

        for (var i = 0; i < count; i++)
            _pointLights[i] = pointLights[i];
    }

    private void BindPbrMaterial(PbrMaterial material)
    {
        _meshShader.SetFloat4("u_BaseColorFactor", material.BaseColorFactor);
        _meshShader.SetFloat("u_MetallicFactor", material.MetallicFactor);
        _meshShader.SetFloat("u_RoughnessFactor", material.RoughnessFactor);
        _meshShader.SetFloat3("u_EmissiveFactor", material.EmissiveFactor);
        _meshShader.SetFloat("u_NormalScale", material.NormalScale);
        _meshShader.SetFloat("u_AoStrength", material.AoStrength);

        _meshShader.SetInt("u_HasBaseColor", material.HasBaseColorMap ? 1 : 0);
        _meshShader.SetInt("u_HasMetallicRoughness", material.HasMetallicRoughnessMap ? 1 : 0);
        _meshShader.SetInt("u_HasNormal", material.HasNormalMap ? 1 : 0);
        _meshShader.SetInt("u_HasAO", material.HasAoMap ? 1 : 0);
        _meshShader.SetInt("u_HasEmissive", material.HasEmissiveMap ? 1 : 0);

        var baseColor = material.BaseColorTexture ?? textureFactory.GetWhiteTexture();
        var mr        = material.MetallicRoughnessTexture ?? textureFactory.GetDefaultMetallicRoughness();
        var normal    = material.NormalTexture ?? textureFactory.GetFlatNormalTexture();
        var ao        = material.AoTexture ?? textureFactory.GetWhiteTexture();
        var emissive  = material.EmissiveTexture ?? textureFactory.GetBlackTexture();

        baseColor.Bind(0);
        mr.Bind(1);
        normal.Bind(2);
        ao.Bind(3);
        emissive.Bind(4);
    }

    private void UploadLightUniforms()
    {
        _meshShader.SetInt("u_DirectionalLightEnabled", _directionalLightEnabled ? 1 : 0);
        _meshShader.SetFloat3("u_DirectionalLightDirection", _directionalLightDirection);
        _meshShader.SetFloat3("u_DirectionalLightColor", _directionalLightColor);
        _meshShader.SetFloat("u_DirectionalLightStrength", _directionalLightStrength);

        _meshShader.SetInt("u_AmbientLightEnabled", _ambientLightEnabled ? 1 : 0);
        _meshShader.SetFloat3("u_AmbientLightColor", _ambientLightColor);
        _meshShader.SetFloat("u_AmbientLightStrength", _ambientLightStrength);

        _meshShader.SetInt("u_PointLightCount", _pointLightCount);
        for (var i = 0; i < _pointLightCount; i++)
        {
            _meshShader.SetFloat3($"u_PointLightPositions[{i}]", _pointLights[i].Position);
            _meshShader.SetFloat3($"u_PointLightColors[{i}]", _pointLights[i].Color);
            _meshShader.SetFloat($"u_PointLightIntensities[{i}]", _pointLights[i].Intensity);
        }
    }

    public void BeginLightVisualization(Camera camera, Matrix4x4 transform)
    {
        if (!Matrix4x4.Invert(transform, out var viewMatrix))
            return;
        var viewProj = viewMatrix * camera.GetProjectionMatrix();
        rendererApi.SetDepthTest(false);
        _lightShader.Bind();
        _lightShader.SetMat4(ViewProjectionUniform, viewProj);
    }

    public void BeginLightVisualization(IViewCamera camera)
    {
        rendererApi.SetDepthTest(false);
        _lightShader.Bind();
        _lightShader.SetMat4(ViewProjectionUniform, camera.GetViewProjectionMatrix());
    }

    public void DrawLightVisualization(Vector3 position, float scale = 0.5f)
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