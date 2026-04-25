using System.Numerics;
using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Renderer.Cameras;
using Engine.Renderer.Shaders;
using Engine.Renderer.Textures;
using Engine.Scene.Components;

namespace Engine.Renderer;

internal sealed class Graphics3D(
    IRendererAPI rendererApi,
    IShaderFactory shaderFactory,
    IMeshFactory meshFactory,
    ITextureFactory textureFactory,
    IFrameBufferFactory frameBufferFactory) : IGraphics3D
{
    private const int MaxPointLights = 16;
    private const string ViewProjectionUniform = "u_ViewProjection";
    // Shadow map parameters matching VictorGordan's working tutorial
    // (YouTubeOpenGL #25: Shadow Maps - Directional Lights).
    private const uint ShadowMapSize = 2048;
    private const float ShadowOrthoSize = 35.0f;
    private const float ShadowNearPlane = 0.1f;
    private const float ShadowFarPlane = 75.0f;
    private const float ShadowLightDistance = 20.0f;
    private IShader _meshShader = null!;
    private IShader _lightShader = null!;
    private IShader _shadowDepthShader = null!;
    private Mesh _cubeMesh = null!;
    private IFrameBuffer _shadowFrameBuffer = null!;
    private Matrix4x4 _lightSpaceMatrix = Matrix4x4.Identity;
    private readonly List<QueuedMeshDraw> _queuedMeshDraws = [];
    private readonly List<QueuedModelDraw> _queuedModelDraws = [];
    private Matrix4x4 _sceneViewProjection = Matrix4x4.Identity;
    private Vector3 _sceneCameraPosition;
    private bool _sceneActive;

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
            "assets/shaders/OpenGL/lightingShader.frag");
        _lightShader = shaderFactory.Create("assets/shaders/OpenGL/lightCubeShader.vert",
            "assets/shaders/OpenGL/lightCubeShader.frag");
        _shadowDepthShader = shaderFactory.Create("assets/shaders/OpenGL/shadowDepth.vert",
            "assets/shaders/OpenGL/shadowDepth.frag");
        _cubeMesh = meshFactory.CreateCube();
        _shadowFrameBuffer = frameBufferFactory.Create(new FrameBufferSpecification(ShadowMapSize, ShadowMapSize)
        {
            AttachmentsSpec = new FramebufferAttachmentSpecification(
            [
                new FramebufferTextureSpecification(FramebufferTextureFormat.DEPTH32F)
            ])
        });

        _meshShader.Bind();
        _meshShader.SetInt("u_DiffuseMap", 0);
        _meshShader.SetInt("u_SpecularMap", 1);
        _meshShader.SetInt("u_NormalMap", 2);
        _meshShader.SetInt("u_ShadowMap", 3);
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
        BeginSceneInternal(viewProj, cameraPos);
    }

    public void BeginScene(IViewCamera camera)
    {
        BeginSceneInternal(camera.GetViewProjectionMatrix(), camera.GetPosition());
    }

    public void EndScene()
    {
        if (!_sceneActive)
            return;

        RenderShadowPass();
        RenderLitPass();
        _meshShader.Unbind();
        _queuedMeshDraws.Clear();
        _queuedModelDraws.Clear();
        _sceneActive = false;
    }

    public void DrawMesh(Matrix4x4 transform, Mesh mesh, MeshMaterial material, int entityId = -1)
    {
        Matrix4x4.Invert(transform, out var invTransform);
        var normalMatrix = Matrix4x4.Transpose(invTransform);

        _queuedMeshDraws.Add(new QueuedMeshDraw(transform, normalMatrix, mesh, material, entityId));
    }

    public void DrawModel(Matrix4x4 transform, MeshComponent meshComponent, ModelRendererComponent modelRenderer,
        int entityId = -1)
    {
        if (meshComponent.Meshes.Count == 0)
            return;

        _queuedModelDraws.Add(new QueuedModelDraw(transform, meshComponent, modelRenderer, entityId));
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

    public void SetShininess(float shininess)
    {
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

    private void BeginSceneInternal(Matrix4x4 viewProjection, Vector3 cameraPosition)
    {
        _sceneViewProjection = viewProjection;
        _sceneCameraPosition = cameraPosition;
        _sceneActive = true;
        _queuedMeshDraws.Clear();
        _queuedModelDraws.Clear();
    }

    private void RenderShadowPass()
    {
        if (!_directionalLightEnabled)
            return;

        var lightDirection = _directionalLightDirection;
        if (lightDirection.LengthSquared() < 0.0001f)
            lightDirection = -Vector3.UnitY;

        lightDirection = Vector3.Normalize(lightDirection);

        // VictorGordan tutorial approach: fixed light position at -dir * distance,
        // looking at world origin, fixed ortho frustum. Stable as camera moves.
        var worldUp = MathF.Abs(Vector3.Dot(lightDirection, Vector3.UnitY)) > 0.99f
            ? Vector3.UnitZ
            : Vector3.UnitY;
        var lightPosition = -lightDirection * ShadowLightDistance;
        var lightView = Matrix4x4.CreateLookAt(lightPosition, Vector3.Zero, worldUp);
        var lightProjection = Matrix4x4.CreateOrthographicOffCenter(
            -ShadowOrthoSize, ShadowOrthoSize, -ShadowOrthoSize, ShadowOrthoSize, ShadowNearPlane, ShadowFarPlane);
        _lightSpaceMatrix = lightView * lightProjection;

        _shadowFrameBuffer.Bind();
        rendererApi.ClearDepth();

        // Cull front faces during shadow pass to eliminate peter-panning
        // (shadow disconnecting from caster). Restored after the pass.
        rendererApi.SetCullFace(CullMode.Front);

        _shadowDepthShader.Bind();
        _shadowDepthShader.SetMat4("u_LightSpaceMatrix", _lightSpaceMatrix);

        foreach (var meshDraw in _queuedMeshDraws)
            DrawDepthMesh(meshDraw.Transform, meshDraw.Mesh);

        foreach (var modelDraw in _queuedModelDraws)
        {
            for (var i = 0; i < modelDraw.MeshComponent.Meshes.Count; i++)
            {
                var mesh = modelDraw.MeshComponent.Meshes[i];
                var meshTransform = mesh.NodeTransform * modelDraw.Transform;
                DrawDepthMesh(meshTransform, mesh);
            }
        }

        _shadowDepthShader.Unbind();
        _shadowFrameBuffer.Unbind();
        rendererApi.SetCullFace(CullMode.None);
    }

    private void RenderLitPass()
    {
        _meshShader.Bind();
        _meshShader.SetMat4(ViewProjectionUniform, _sceneViewProjection);
        UploadLightUniforms();
        _meshShader.SetFloat3("u_ViewPosition", _sceneCameraPosition);
        _meshShader.SetMat4("u_LightSpaceMatrix", _lightSpaceMatrix);
        rendererApi.BindTexture2D(_shadowFrameBuffer.GetDepthAttachmentRendererId(), 3);

        foreach (var meshDraw in _queuedMeshDraws)
        {
            DrawLitMesh(
                meshDraw.Transform,
                meshDraw.NormalMatrix,
                meshDraw.Mesh,
                meshDraw.Material,
                Vector4.One,
                meshDraw.EntityId);
        }

        foreach (var modelDraw in _queuedModelDraws)
        {
            for (var i = 0; i < modelDraw.MeshComponent.Meshes.Count; i++)
            {
                var mesh = modelDraw.MeshComponent.Meshes[i];
                var meshTransform = mesh.NodeTransform * modelDraw.Transform;
                Matrix4x4.Invert(meshTransform, out var invTransform);
                var normalMatrix = Matrix4x4.Transpose(invTransform);
                var material = i < modelDraw.ModelRenderer.Materials.Count
                    ? modelDraw.ModelRenderer.Materials[i]
                    : new MeshMaterial();

                DrawLitMesh(
                    meshTransform,
                    normalMatrix,
                    mesh,
                    material,
                    modelDraw.ModelRenderer.Color,
                    modelDraw.EntityId);
            }
        }
    }

    private void DrawDepthMesh(Matrix4x4 transform, Mesh mesh)
    {
        _shadowDepthShader.SetMat4("u_Model", transform);
        mesh.Bind();
        rendererApi.DrawIndexed(mesh.GetVertexArray(), (uint)mesh.GetIndexCount());
    }

    private void DrawLitMesh(Matrix4x4 transform, Matrix4x4 normalMatrix, Mesh mesh, MeshMaterial material, Vector4 color,
        int entityId)
    {
        _meshShader.SetMat4("u_Model", transform);
        _meshShader.SetMat4("u_NormalMatrix", normalMatrix);
        _meshShader.SetFloat4("u_Color", color);
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
        _shadowDepthShader?.Dispose();
        _shadowDepthShader = null!;
        _shadowFrameBuffer?.Dispose();
        _shadowFrameBuffer = null!;
        _cubeMesh?.Dispose();
        _cubeMesh = null!;

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private readonly record struct QueuedMeshDraw(
        Matrix4x4 Transform,
        Matrix4x4 NormalMatrix,
        Mesh Mesh,
        MeshMaterial Material,
        int EntityId);

    private readonly record struct QueuedModelDraw(
        Matrix4x4 Transform,
        MeshComponent MeshComponent,
        ModelRendererComponent ModelRenderer,
        int EntityId);
}