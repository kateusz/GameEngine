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
    private const uint PointShadowMapSize = 512;
    private const int ShadowMapTextureSlot = 3;
    private const int PointShadowMapTextureSlot = 4;

    private static readonly ILogger Logger = Log.ForContext<Graphics3D>();

    private IShader _cubeShader = null!;
    private IShader _modelShader = null!;
    private IShader _skyboxShader = null!;
    private IShader? _depthShader;
    private IShader? _pointDepthShader;
    private IFrameBuffer? _shadowFramebuffer;
    private IFrameBuffer? _pointShadowFramebuffer;
    private Mesh _cubeMesh = null!;
    private IVertexArray _skyboxVertexArray = null!;

    private Matrix4x4 _viewProjection = Matrix4x4.Identity;
    private Vector3 _viewPosition;
    private Vector3 _ambientColor = Vector3.One;
    private float _ambientStrength = 0.1f;
    private Vector3 _lightDirection = new(0, -1, 0);
    private Vector3 _lightColor = Vector3.Zero;
    private PointLightUniform[] _pointLights = [];
    private SpotLightUniform[] _spotLights = [];
    private Matrix4x4 _lightSpaceMatrix = Matrix4x4.Identity;
    private bool _shadowsEnabled;
    private bool _shadowsAvailable;
    private bool _pointShadowMapReady;
    private bool _pointShadowsAvailable;
    private bool _inShadowPass;
    private bool _inPointShadowPass;
    private Matrix4x4[] _pointShadowFaceMatrices = [];

    private IShader? _boundShader;
    private bool _cubeSceneUniformsUploaded;
    private bool _modelSceneUniformsUploaded;
    private Frustum _frustum;

    private readonly Statistics _stats = new();
    private bool _disposed;

    public void Init()
    {
        DrainGlErrors();
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
        _modelShader.SetInt("u_PointShadowMap", PointShadowMapTextureSlot);
        _modelShader.Unbind();

        _cubeShader.Bind();
        _cubeShader.SetInt("u_ShadowMap", ShadowMapTextureSlot);
        _cubeShader.SetInt("u_PointShadowMap", PointShadowMapTextureSlot);
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
        if (_boundShader != null)
        {
            _boundShader.Unbind();
            _boundShader = null;
            _cubeSceneUniformsUploaded = false;
            _modelSceneUniformsUploaded = false;
        }

        _pointShadowMapReady = false;
    }

    public bool BeginShadowPass(Matrix4x4 lightSpaceMatrix)
    {
        if (!_shadowsAvailable || _depthShader == null || _shadowFramebuffer == null)
            return false;

        _lightSpaceMatrix = lightSpaceMatrix;
        _shadowFramebuffer.Bind();
        rendererApi.Clear();
        rendererApi.SetFaceCulling(true, cullFrontFaces: true);

        _depthShader.Bind();
        _boundShader = _depthShader;
        _depthShader.SetMat4("u_LightSpaceMatrix", lightSpaceMatrix);

        _inShadowPass = true;
        return true;
    }

    public void EndShadowPass()
    {
        if (!_inShadowPass)
            return;

        _shadowFramebuffer?.Unbind();
        rendererApi.SetFaceCulling(true, cullFrontFaces: false);

        _inShadowPass = false;
        _boundShader = null;
    }

    public bool BeginPointShadowPass(Vector3 lightPosition, float farPlane, int face)
    {
        if (!_pointShadowsAvailable || _pointDepthShader == null || _pointShadowFramebuffer == null)
            return false;
        if (face is < 0 or > 5)
            return false;

        if (face == 0)
        {
            _pointShadowFaceMatrices = LightSpaceMatrix.CreateCubemapFaces(lightPosition, farPlane);
            _pointShadowFramebuffer.Bind();
            rendererApi.SetFaceCulling(true, cullFrontFaces: false);

            _pointDepthShader.Bind();
            _boundShader = _pointDepthShader;
            _pointDepthShader.SetFloat3("u_LightPos", lightPosition);
            _pointDepthShader.SetFloat("u_FarPlane", farPlane);
            _inPointShadowPass = true;
        }

        _pointShadowFramebuffer.BindDepthCubemapFace(face);
        rendererApi.Clear();
        _pointDepthShader!.SetMat4("u_ShadowMatrix", _pointShadowFaceMatrices[face]);
        return true;
    }

    public void EndPointShadowPass()
    {
        if (!_inPointShadowPass)
            return;

        _pointShadowFramebuffer?.Unbind();
        rendererApi.SetFaceCulling(true, cullFrontFaces: false);

        _pointShadowMapReady = true;
        _inPointShadowPass = false;
        _boundShader = null;
    }

    public void DrawCube(Matrix4x4 transform, Vector4 color, int entityId = -1, Texture2D? texture = null,
        float tilingFactor = 1.0f)
    {
        if (_inShadowPass || _inPointShadowPass)
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
        if (_inShadowPass || _inPointShadowPass)
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

    public void SetPointLights(PointLightUniform[] lights)
    {
        _pointLights = lights ?? [];
        InvalidateSceneUniforms();
    }

    public void SetSpotLights(SpotLightUniform[] lights)
    {
        _spotLights = lights ?? [];
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
        var depthShader = _inPointShadowPass ? _pointDepthShader : _depthShader;
        if (mesh.GetIndexCount() == 0 || depthShader == null)
            return;

        depthShader.SetMat4("u_Model", transform);
        mesh.Bind();
        rendererApi.DrawIndexed(mesh.GetVertexArray(), (uint)mesh.GetIndexCount());
        _stats.DrawCalls++;
    }

    private void DrainGlErrors()
    {
        while (rendererApi.GetError() != 0)
        {
        }
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
            Logger.Warning(ex, "Directional shadow mapping disabled: failed to create shadow framebuffer or depth shader");
            _shadowsAvailable = false;
            _shadowFramebuffer?.Dispose();
            _shadowFramebuffer = null;
            _depthShader?.Dispose();
            _depthShader = null;
        }

        try
        {
            var cubemapSpec = new FrameBufferTextureSpecification(FrameBufferTextureFormat.DepthCubemap)
            {
                Filter = FrameBufferTextureFilter.Nearest,
                Wrap = FrameBufferTextureWrap.ClampToEdge
            };
            _pointShadowFramebuffer = frameBufferFactory.Create(new FrameBufferSpecification(
                PointShadowMapSize, PointShadowMapSize)
            {
                AttachmentsSpec = new FrameBufferAttachmentSpecification([cubemapSpec])
            });
            _pointDepthShader = shaderFactory.Create(
                PathBuilder.Resolve("assets/shaders/OpenGL/pointShadowDepth.vert"),
                PathBuilder.Resolve("assets/shaders/OpenGL/pointShadowDepth.frag"));
            _pointShadowsAvailable = true;
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Point shadow mapping disabled: failed to create cubemap framebuffer or depth shader");
            _pointShadowsAvailable = false;
            _pointShadowFramebuffer?.Dispose();
            _pointShadowFramebuffer = null;
            _pointDepthShader?.Dispose();
            _pointDepthShader = null;
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

        if (_inShadowPass || _inPointShadowPass)
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

        UploadSceneUniforms(shader);
        if (isModelShader)
            _modelSceneUniformsUploaded = true;
        else
            _cubeSceneUniformsUploaded = true;
    }

    private void UploadSceneUniforms(IShader shader)
    {
        shader.SetMat4(ViewProjectionUniform, _viewProjection);
        shader.SetMat4("u_LightSpaceMatrix", _lightSpaceMatrix);
        shader.SetFloat3("u_ViewPosition", _viewPosition);
        shader.SetFloat3("u_AmbientColor", _ambientColor);
        shader.SetFloat("u_AmbientStrength", _ambientStrength);
        shader.SetFloat3("u_LightDirection", _lightDirection);
        shader.SetFloat3("u_LightColor", _lightColor);
        shader.SetInt("u_ShadowsEnabled", _shadowsEnabled ? 1 : 0);
        shader.SetInt("u_PointShadowsEnabled", _pointShadowMapReady && _pointLights.Length > 0 ? 1 : 0);
        UploadPointLights(shader);
        UploadSpotLights(shader);
        if (_shadowsEnabled)
            rendererApi.BindTexture2D(_shadowFramebuffer!.GetDepthAttachmentRendererId(), ShadowMapTextureSlot);
        if (_pointShadowMapReady)
            rendererApi.BindTextureCube(_pointShadowFramebuffer!.GetDepthAttachmentRendererId(), PointShadowMapTextureSlot);
    }

    private void UploadPointLights(IShader shader)
    {
        var count = System.Math.Min(_pointLights.Length, LightingMath.MaxPointLights);
        shader.SetInt("u_PointLightCount", count);
        for (var i = 0; i < count; i++)
        {
            var light = _pointLights[i];
            shader.SetFloat3($"u_PointLights[{i}].position", light.Position);
            shader.SetFloat3($"u_PointLights[{i}].color", light.Color);
            shader.SetFloat($"u_PointLights[{i}].constant", light.Constant);
            shader.SetFloat($"u_PointLights[{i}].linear", light.Linear);
            shader.SetFloat($"u_PointLights[{i}].quadratic", light.Quadratic);
            shader.SetFloat($"u_PointLights[{i}].range", light.Range);
        }
    }

    private void UploadSpotLights(IShader shader)
    {
        var count = System.Math.Min(_spotLights.Length, LightingMath.MaxSpotLights);
        shader.SetInt("u_SpotLightCount", count);
        for (var i = 0; i < count; i++)
        {
            var light = _spotLights[i];
            shader.SetFloat3($"u_SpotLights[{i}].position", light.Position);
            shader.SetFloat3($"u_SpotLights[{i}].direction", light.Direction);
            shader.SetFloat3($"u_SpotLights[{i}].color", light.Color);
            shader.SetFloat($"u_SpotLights[{i}].constant", light.Constant);
            shader.SetFloat($"u_SpotLights[{i}].linear", light.Linear);
            shader.SetFloat($"u_SpotLights[{i}].quadratic", light.Quadratic);
            shader.SetFloat($"u_SpotLights[{i}].innerCos", light.InnerCutoffCos);
            shader.SetFloat($"u_SpotLights[{i}].outerCos", light.OuterCutoffCos);
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

        EndShadowPass();
        EndPointShadowPass();
        EndScene();

        textureFactory.GetWhiteTexture().Bind(PointShadowMapTextureSlot);
        DrainGlErrors();

        _cubeShader?.Dispose();
        _cubeShader = null!;
        _modelShader?.Dispose();
        _modelShader = null!;
        _skyboxShader?.Dispose();
        _skyboxShader = null!;
        _depthShader?.Dispose();
        _depthShader = null;
        _pointDepthShader?.Dispose();
        _pointDepthShader = null;
        _shadowFramebuffer?.Dispose();
        _shadowFramebuffer = null;
        _pointShadowFramebuffer?.Dispose();
        _pointShadowFramebuffer = null;
        _cubeMesh?.Dispose();
        _cubeMesh = null!;

        DrainGlErrors();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
