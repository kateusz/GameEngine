using System.Numerics;
using Engine.Core;
using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Meshes;
using Engine.Renderer.Shaders;
using Engine.Renderer.Textures;
using Serilog;

namespace Engine.Renderer.Pipeline;

internal sealed class Graphics3D(
    IRendererAPI rendererApi,
    IShaderFactory shaderFactory,
    IMeshFactory meshFactory,
    ITextureFactory textureFactory,
    IVertexArrayFactory vertexArrayFactory,
    IFrameBufferFactory frameBufferFactory) : IGraphics3D
{
    private const string ViewProjectionUniform = "u_ViewProjection";
    private const uint ShadowMapSize = 1024;
    private const int ShadowMapTextureSlot = 3;

    private static readonly ILogger Logger = Log.ForContext<Graphics3D>();

    private IShader _cubeShader = null!;
    private IShader _modelShader = null!;
    private IShader _skyboxShader = null!;
    private IShader? _depthShader;
    private IFrameBuffer? _shadowFramebuffer;
    private Mesh _cubeMesh = null!;
    private IVertexArray _skyboxVertexArray = null!;

    private Matrix4x4 _viewProjection = Matrix4x4.Identity;
    private Vector3 _viewPosition;
    private Vector3 _ambientColor = Vector3.One;
    private float _ambientStrength = 0.1f;
    private Vector3 _lightDirection = new(0, -1, 0);
    private Vector3 _lightColor = Vector3.Zero;
    private Matrix4x4 _lightSpaceMatrix = Matrix4x4.Identity;
    private bool _shadowsEnabled;
    private bool _shadowsAvailable;
    private bool _shadowPassActive;
    private bool _inShadowPass;
    private bool _skipFrustumCulling;

    private IShader? _boundShader;
    private bool _cubeSceneUniformsUploaded;
    private bool _modelSceneUniformsUploaded;
    private Frustum _frustum;

    private readonly Statistics _stats = new();
    private bool _disposed;

    public void Init()
    {
        _cubeShader = shaderFactory.Create(
            PathBuilder.Resolve("assets/shaders/OpenGL/cube.vert"),
            PathBuilder.Resolve("assets/shaders/OpenGL/cube.frag"));
        _modelShader = shaderFactory.Create(
            PathBuilder.Resolve("assets/shaders/OpenGL/modelShader.vert"),
            PathBuilder.Resolve("assets/shaders/OpenGL/modelShader.frag"));
        _skyboxShader = shaderFactory.Create(
            PathBuilder.Resolve("assets/shaders/OpenGL/skybox.vert"),
            PathBuilder.Resolve("assets/shaders/OpenGL/skybox.frag"));
        _cubeMesh = meshFactory.CreateCube();
        _skyboxVertexArray = vertexArrayFactory.Create();

        _modelShader.Bind();
        _modelShader.SetInt("u_DiffuseMap", 0);
        _modelShader.SetInt("u_SpecularMap", 1);
        _modelShader.SetInt("u_NormalMap", 2);
        _modelShader.SetInt("u_ShadowMap", ShadowMapTextureSlot);
        _modelShader.Unbind();

        _cubeShader.Bind();
        _cubeShader.SetInt("u_ShadowMap", ShadowMapTextureSlot);
        _cubeShader.Unbind();

        _skyboxShader.Bind();
        _skyboxShader.SetInt("u_EquirectMap", 0);
        _skyboxShader.Unbind();

        TryInitShadowResources();
    }

    public void BeginScene(in SceneView view)
    {
        _viewProjection = view.ViewProjection;
        _viewPosition = view.ViewPosition;
        BeginSceneState();
    }

    public void EndScene()
    {
        if (_boundShader == null)
            return;

        _boundShader.Unbind();
        _boundShader = null;
        _cubeSceneUniformsUploaded = false;
        _modelSceneUniformsUploaded = false;
    }

    public bool BeginShadowPass(Matrix4x4 lightSpaceMatrix)
    {
        if (!_shadowsAvailable || _depthShader == null || _shadowFramebuffer == null)
            return false;

        _shadowFramebuffer.Bind();
        rendererApi.Clear();
        rendererApi.SetFaceCulling(true, cullFrontFaces: true);

        _depthShader.Bind();
        _boundShader = _depthShader;
        _depthShader.SetMat4("u_LightSpaceMatrix", lightSpaceMatrix);

        _inShadowPass = true;
        _skipFrustumCulling = true;
        _shadowPassActive = true;
        return true;
    }

    public void EndShadowPass()
    {
        if (!_shadowPassActive)
            return;

        _shadowFramebuffer?.Unbind();
        rendererApi.SetFaceCulling(true, cullFrontFaces: false);

        _inShadowPass = false;
        _skipFrustumCulling = false;
        _shadowPassActive = false;
        _boundShader = null;
    }

    public void DrawCube(Matrix4x4 transform, Vector4 color, int entityId = -1, Texture2D? texture = null,
        float tilingFactor = 1.0f)
    {
        if (_inShadowPass)
        {
            DrawDepth(_cubeMesh, transform);
            return;
        }

        if (ShouldSkipDraw(transform, _cubeMesh))
            return;

        EnsureShaderBound(_cubeShader, isModelShader: false);
        BindPerDraw(_cubeShader, transform, color, entityId);

        _cubeShader.SetFloat("u_TilingFactor", tilingFactor);
        _cubeShader.SetInt("u_UseTexture", texture != null ? 1 : 0);
        if (texture != null)
        {
            texture.Bind(0);
            _cubeShader.SetInt("u_Texture", 0);
        }

        _cubeMesh.Bind();
        rendererApi.DrawIndexed(_cubeMesh.GetVertexArray(), (uint)_cubeMesh.GetIndexCount());
        _stats.DrawCalls++;
    }

    public void DrawMesh(Matrix4x4 transform, Mesh mesh, Vector4 tint, int entityId = -1)
    {
        if (_inShadowPass)
        {
            DrawDepth(mesh, transform);
            return;
        }

        if (ShouldSkipDraw(transform, mesh))
            return;

        EnsureShaderBound(_modelShader, isModelShader: true);
        BindPerDraw(_modelShader, transform, tint, entityId);

        _modelShader.SetFloat("u_Shininess", mesh.Shininess);
        _modelShader.SetInt("u_HasDiffuseMap", mesh.HasDiffuseMap ? 1 : 0);
        _modelShader.SetInt("u_HasSpecularMap", mesh.HasSpecularMap ? 1 : 0);
        _modelShader.SetInt("u_HasNormalMap", mesh.HasNormalMap ? 1 : 0);

        (mesh.DiffuseTexture ?? textureFactory.GetWhiteTexture()).Bind(0);
        (mesh.SpecularTexture ?? textureFactory.GetBlackTexture()).Bind(1);
        (mesh.NormalTexture ?? textureFactory.GetFlatNormalTexture()).Bind(2);

        mesh.Bind();
        rendererApi.DrawIndexed(mesh.GetVertexArray(), (uint)mesh.GetIndexCount());
        _stats.DrawCalls++;
    }

    public void SetAmbientLight(Vector3 color, float strength)
    {
        _ambientColor = color;
        _ambientStrength = strength;
        InvalidateSceneUniforms();
    }

    public void SetDirectionalLight(Vector3 direction, Vector3 color, Matrix4x4? lightSpaceMatrix = null)
    {
        _lightDirection = direction;
        _lightColor = color;
        _shadowsEnabled = lightSpaceMatrix.HasValue && _shadowsAvailable;
        _lightSpaceMatrix = lightSpaceMatrix ?? Matrix4x4.Identity;
        InvalidateSceneUniforms();
    }

    public void DrawSkybox(Texture2D hdrTexture, float intensity, float yawRadians)
    {
        if (_boundShader != _skyboxShader)
        {
            _skyboxShader.Bind();
            _boundShader = _skyboxShader;
        }

        if (!Matrix4x4.Invert(_viewProjection, out var inverseVp))
            return;

        _skyboxShader.SetMat4("u_InverseViewProjection", inverseVp);
        _skyboxShader.SetFloat("u_Intensity", intensity);
        _skyboxShader.SetFloat("u_Yaw", yawRadians);

        hdrTexture.Bind(0);

        rendererApi.SetDepthWrite(false);
        _skyboxVertexArray.Bind();
        rendererApi.DrawArrays(_skyboxVertexArray, 3);
        rendererApi.SetDepthWrite(true);

        _stats.DrawCalls++;
    }

    private void DrawDepth(Mesh mesh, Matrix4x4 transform)
    {
        if (mesh.GetIndexCount() == 0 || _depthShader == null)
            return;

        _depthShader.SetMat4("u_Model", transform);
        mesh.Bind();
        rendererApi.DrawIndexed(mesh.GetVertexArray(), (uint)mesh.GetIndexCount());
        _stats.DrawCalls++;
    }

    private void TryInitShadowResources()
    {
        try
        {
            var depthSpec = new FrameBufferTextureSpecification(FrameBufferTextureFormat.DepthComponent)
            {
                Filter = FrameBufferTextureFilter.Nearest,
                Wrap = FrameBufferTextureWrap.ClampToBorder
            };
            _shadowFramebuffer = frameBufferFactory.Create(new FrameBufferSpecification(ShadowMapSize, ShadowMapSize)
            {
                AttachmentsSpec = new FrameBufferAttachmentSpecification([depthSpec])
            });
            _depthShader = shaderFactory.Create(
                PathBuilder.Resolve("assets/shaders/OpenGL/shadowDepth.vert"),
                PathBuilder.Resolve("assets/shaders/OpenGL/shadowDepth.frag"));
            _shadowsAvailable = true;
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Shadow mapping disabled: failed to create shadow framebuffer or depth shader");
            _shadowsAvailable = false;
            _shadowFramebuffer?.Dispose();
            _shadowFramebuffer = null;
            _depthShader?.Dispose();
            _depthShader = null;
        }
    }

    private void BeginSceneState()
    {
        rendererApi.SetDepthTest(true);
        _boundShader = null;
        _cubeSceneUniformsUploaded = false;
        _modelSceneUniformsUploaded = false;
        _frustum = Frustum.FromViewProjection(_viewProjection);
    }

    private bool ShouldSkipDraw(Matrix4x4 transform, Mesh mesh)
    {
        if (mesh.GetIndexCount() == 0)
            return true;

        if (_skipFrustumCulling)
            return false;

        if (mesh.LocalAabb is not { } local)
            return false;

        var world = local.Transform(transform);
        if (!world.IsFinite || _frustum.Intersects(world))
            return false;

        _stats.CulledDraws++;
        return true;
    }

    private void InvalidateSceneUniforms()
    {
        _cubeSceneUniformsUploaded = false;
        _modelSceneUniformsUploaded = false;
    }

    private void EnsureShaderBound(IShader shader, bool isModelShader)
    {
        if (_boundShader != shader)
        {
            shader.Bind();
            _boundShader = shader;
        }

        var uploaded = isModelShader ? _modelSceneUniformsUploaded : _cubeSceneUniformsUploaded;
        if (uploaded)
            return;

        UploadSceneUniforms(shader, isModelShader);
        if (isModelShader)
            _modelSceneUniformsUploaded = true;
        else
            _cubeSceneUniformsUploaded = true;
    }

    private void UploadSceneUniforms(IShader shader, bool isModelShader)
    {
        shader.SetMat4(ViewProjectionUniform, _viewProjection);
        if (isModelShader)
            shader.SetFloat3("u_ViewPosition", _viewPosition);
        shader.SetFloat3("u_AmbientColor", _ambientColor);
        shader.SetFloat("u_AmbientStrength", _ambientStrength);
        shader.SetFloat3("u_LightDirection", _lightDirection);
        shader.SetFloat3("u_LightColor", _lightColor);
        shader.SetInt("u_ShadowsEnabled", _shadowsEnabled ? 1 : 0);
        if (_shadowsEnabled)
        {
            shader.SetMat4("u_LightSpaceMatrix", _lightSpaceMatrix);
            rendererApi.BindTexture2D(_shadowFramebuffer!.GetDepthAttachmentRendererId(), ShadowMapTextureSlot);
        }
    }

    private static void BindPerDraw(IShader shader, Matrix4x4 transform, Vector4 color, int entityId)
    {
        shader.SetMat4("u_Model", transform);
        shader.SetMat4("u_NormalMatrix", ComputeNormalMatrix(transform));
        shader.SetFloat4("u_Color", color);
        shader.SetInt("u_EntityID", entityId);
    }

    private static Matrix4x4 ComputeNormalMatrix(Matrix4x4 model) =>
        Matrix4x4.Invert(model, out var inv) ? Matrix4x4.Transpose(inv) : Matrix4x4.Identity;

    public void ResetStats()
    {
        _stats.DrawCalls = 0;
        _stats.CulledDraws = 0;
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
        _modelShader?.Dispose();
        _modelShader = null!;
        _skyboxShader?.Dispose();
        _skyboxShader = null!;
        _depthShader?.Dispose();
        _depthShader = null;
        _shadowFramebuffer?.Dispose();
        _shadowFramebuffer = null;
        _cubeMesh?.Dispose();
        _cubeMesh = null!;

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
