using System.Numerics;
using Engine.Core;
using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Meshes;
using Engine.Renderer.Shaders;
using Engine.Renderer.Textures;
using Engine.Renderer.Textures.EnvironmentMap;
using Engine.Scene.Cameras;

namespace Engine.Renderer.Pipeline;

internal sealed class Graphics3D(
    IRendererAPI rendererApi,
    IShaderFactory shaderFactory,
    IMeshFactory meshFactory,
    ITextureFactory textureFactory,
    IEnvironmentMapFactory environmentMapFactory,
    IFrameBufferFactory frameBufferFactory,
    IVertexArrayFactory vertexArrayFactory) : IGraphics3D
{
    private const string ViewProjectionUniform = "u_ViewProjection";
    private const uint ShadowMapSize = 1024;
    private const uint PointShadowMapSize = 512;
    private const float ShadowMaxDistance = 80f;
    private const float ShadowNearPad = 1f;
    private const int ShadowMapSlot = 6;
    private const int PointShadowMapSlot = 7;
    private const int EmissiveMapSlot = 8;
    private const float PointShadowNear = 1f;

    private IShader _cubeShader = null!;
    private IShader _texturedShader = null!;
    private IShader _skyboxShader = null!;
    private IVertexArray _skyboxVao = null!;
    private IShader _depthShader = null!;
    private IShader? _pointDepthShader;
    private IShader? _wireframeShader;
    private Mesh _cubeMesh = null!;
    private IFrameBuffer? _shadowMap;
    private IFrameBuffer? _pointShadowMap;

    private Vector3 _ambientColor = Vector3.One;
    private float _ambientStrength = 0.35f;
    private Vector3 _lightDirection = new(0, -1, 0);
    private Vector3 _lightColor = Vector3.Zero;
    private float _lightStrength = 1f;
    private Vector3 _pointLightPosition;
    private Vector3 _pointLightColor = Vector3.Zero;
    private float _pointLightRange = 25f;

    private EnvironmentMap? _environmentMap;
    private float _envIntensity = 1f;
    private Matrix4x4 _view = Matrix4x4.Identity;
    private Matrix4x4 _projection = Matrix4x4.Identity;
    private Matrix4x4 _viewProjection = Matrix4x4.Identity;
    private Vector3 _viewPosition;
    private bool _wireframe;
    private bool _wireframeLoadFailed;
    private bool _shadowPass;
    private bool _shadowEnabled;
    private bool _pointShadowPass;
    private bool _pointShadowsEnabled;
    private Matrix4x4 _lightSpaceMatrix = Matrix4x4.Identity;
    private readonly Matrix4x4[] _pointShadowMatrices = new Matrix4x4[6];
    private readonly Statistics _stats = new();
    private bool _disposed;

    private static readonly MeshMaterial BuiltinSphereMaterial = new();
    private static readonly Vector3[] ClipCorners =
    [
        new(-1f, -1f, 0f), new(1f, -1f, 0f), new(-1f, 1f, 0f), new(1f, 1f, 0f),
        new(-1f, -1f, 1f), new(1f, -1f, 1f), new(-1f, 1f, 1f), new(1f, 1f, 1f)
    ];

    public void Init()
    {
        _cubeShader = shaderFactory.Create(
            PathBuilder.Resolve("assets/shaders/OpenGL/flatColorShader.vert"),
            PathBuilder.Resolve("assets/shaders/OpenGL/flatColorShader.frag"));
        _texturedShader = shaderFactory.Create(
            PathBuilder.Resolve("assets/shaders/OpenGL/lightingShader.vert"),
            PathBuilder.Resolve("assets/shaders/OpenGL/lightingShader.frag"),
            [new ShaderDefine("MAX_REFLECTION_LOD",
                EnvironmentMapConstants.MaxReflectionLod.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture))]);
        _skyboxShader = shaderFactory.Create(
            ResolveHostShader("skybox.vert"),
            ResolveHostShader("skybox.frag"));
        _skyboxVao = vertexArrayFactory.Create();
        _cubeMesh = meshFactory.CreateCube();

        // Texture unit contract (must match the sampler uniforms in lightingShader.frag):
        // 0 albedo, 1 metallicRoughness, 2 normal, 3 irradiance, 4 prefilter, 5 brdf LUT,
        // 6 directional shadow, 7 point shadow, 8 emissive
        _texturedShader.Bind();
        _texturedShader.SetInt("u_AlbedoMap", 0);
        _texturedShader.SetInt("u_MetallicRoughnessMap", 1);
        _texturedShader.SetInt("u_NormalMap", 2);
        _texturedShader.SetInt("u_IrradianceMap", 3);
        _texturedShader.SetInt("u_PrefilterMap", 4);
        _texturedShader.SetInt("u_BrdfLut", 5);
        _texturedShader.SetInt("u_ShadowMap", ShadowMapSlot);
        _texturedShader.SetInt("u_PointShadowMap", PointShadowMapSlot);
        _texturedShader.SetInt("u_EmissiveMap", EmissiveMapSlot);
        _texturedShader.Unbind();

        _cubeShader.Bind();
        _cubeShader.SetInt("u_ShadowMap", ShadowMapSlot);
        _cubeShader.SetInt("u_PointShadowMap", PointShadowMapSlot);
        _cubeShader.Unbind();

        _depthShader = shaderFactory.Create(
            ResolveHostShader("shadowMappingDepth.vert"),
            ResolveHostShader("shadowMappingDepth.frag"));
        _depthShader.Bind();
        _depthShader.SetInt("u_AlbedoMap", 0);
        _depthShader.Unbind();

        _skyboxShader.Bind();
        _skyboxShader.SetInt("u_Skybox", 0);
        _skyboxShader.Unbind();
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

        ApplyCamera(viewMatrix, camera.GetProjectionMatrix(), new Vector3(transform.M41, transform.M42, transform.M43));
    }

    public void BeginScene(IViewCamera camera) =>
        ApplyCamera(camera.GetViewMatrix(), camera.GetProjectionMatrix(), camera.GetPosition());

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

    public void DrawCube(Matrix4x4 transform, Vector4 color, int entityId = -1, Texture2D? albedo = null, float metallic = 0f, float roughness = 0.5f)
    {
        if (albedo != null)
        {
            DrawMesh(transform, _cubeMesh, BuiltinSphereMaterial, color, metallic, roughness, entityId, albedoOverride: albedo);
            return;
        }

        rendererApi.SetDepthTest(true);

        if (_shadowPass)
        {
            DrawShadowCaster(_cubeMesh, transform);
            return;
        }

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

    public void DrawBuiltinSphere(Matrix4x4 transform, Vector4 tint, float metallic, float roughness, int entityId = -1, Texture2D? albedo = null) =>
        DrawMesh(transform, meshFactory.CreateSphere(), BuiltinSphereMaterial, tint, metallic, roughness, entityId, albedoOverride: albedo);

    public void DrawMesh(
        Matrix4x4 transform,
        Mesh mesh,
        MeshMaterial material,
        Vector4 tint,
        float metallic,
        float roughness,
        int entityId = -1,
        Matrix4x4[]? bonePalette = null,
        Texture2D? albedoOverride = null)
    {
        rendererApi.SetDepthTest(true);

        if (_shadowPass)
        {
            DrawShadowCaster(mesh, transform, material, material.ResolveBaseColor(tint), bonePalette, albedoOverride);
            return;
        }

        if (_wireframe)
        {
            DrawWireframe(mesh, transform, entityId, bonePalette);
            return;
        }

        var baseColor = material.ResolveBaseColor(tint);
        var albedoMap = albedoOverride ?? material.AlbedoTexture;
        BindCommon(_texturedShader, transform, baseColor, entityId, bonePalette);
        _texturedShader.SetFloat("u_Metallic", metallic);
        _texturedShader.SetFloat("u_Roughness", roughness);
        _texturedShader.SetInt("u_HasAlbedoMap", albedoMap != null ? 1 : 0);
        _texturedShader.SetInt("u_HasMetallicRoughnessMap", material.HasMetallicRoughnessMap ? 1 : 0);
        _texturedShader.SetInt("u_HasNormalMap", material.HasNormalMap ? 1 : 0);
        _texturedShader.SetInt("u_HasEmissiveMap", material.HasEmissiveMap ? 1 : 0);
        _texturedShader.SetFloat3("u_EmissiveFactor", material.EmissiveFactor);
        _texturedShader.SetInt("u_AlphaMode", (int)material.AlphaMode);
        _texturedShader.SetFloat("u_AlphaCutoff", material.AlphaCutoff);

        _texturedShader.SetInt("u_UseIBL", _environmentMap is not null ? 1 : 0);
        _texturedShader.SetFloat("u_IblIntensity", _envIntensity);
        if (_environmentMap is not null)
            environmentMapFactory.GetBrdfLut().Bind(5);
        else
            textureFactory.GetWhiteTexture().Bind(5);

        (albedoMap ?? textureFactory.GetWhiteTexture()).Bind(0);
        (material.MetallicRoughnessTexture ?? textureFactory.GetWhiteTexture()).Bind(1);
        (material.NormalTexture ?? textureFactory.GetFlatNormalTexture()).Bind(2);
        (material.EmissiveTexture ?? textureFactory.GetBlackTexture()).Bind(EmissiveMapSlot);

        rendererApi.SetFaceCulling(!material.DoubleSided);
        try
        {
            mesh.Bind();
            rendererApi.DrawIndexed(mesh.GetVertexArray(), (uint)mesh.GetIndexCount());
            _stats.DrawCalls++;
        }
        finally
        {
            rendererApi.SetFaceCulling(true);
        }

        _texturedShader.Unbind();
    }

    public void DrawSkybox()
    {
        if (_environmentMap is null || _wireframe)
            return;

        rendererApi.SetDepthTest(false);
        rendererApi.SetDepthWrite(false);
        rendererApi.SetFaceCulling(false);
        try
        {
            var viewProj = RotationOnly(_view) * _projection;
            if (!Matrix4x4.Invert(viewProj, out var invViewProj))
                return;

            _environmentMap.Environment.Bind(0);
            _skyboxShader.Bind();
            _skyboxShader.SetMat4("u_InverseViewProjection", invViewProj);
            _skyboxShader.SetFloat("u_Intensity", _envIntensity);
            rendererApi.DrawArrays(_skyboxVao, 3);
            _stats.DrawCalls++;
            _skyboxShader.Unbind();
        }
        finally
        {
            rendererApi.SetFaceCulling(true);
            rendererApi.SetDepthWrite(true);
            rendererApi.SetDepthTest(true);
        }
    }

    private static Matrix4x4 RotationOnly(Matrix4x4 view) => new(
        view.M11, view.M12, view.M13, 0f,
        view.M21, view.M22, view.M23, 0f,
        view.M31, view.M32, view.M33, 0f,
        0f, 0f, 0f, 1f);

    public void SetAmbientLight(Vector3 color, float strength)
    {
        _ambientColor = color;
        _ambientStrength = strength;
    }

    public void SetDirectionalLight(Vector3 direction, Vector3 color, float strength)
    {
        _lightDirection = direction;
        _lightColor = color;
        _lightStrength = strength;
    }

    public bool BeginShadowPass()
    {
        _shadowPass = false;
        _shadowEnabled = !_wireframe && _lightColor.LengthSquared() * (_lightStrength * _lightStrength) > 1e-10f;
        if (!_shadowEnabled)
            return false;

        EnsureShadowMap();
        _lightSpaceMatrix = ComputeLightSpaceMatrix();
        _shadowPass = true;
        _shadowMap!.Bind();
        rendererApi.SetDepthTest(true);
        rendererApi.Clear();
        _depthShader.Bind();
        _depthShader.SetMat4("u_LightSpaceMatrix", _lightSpaceMatrix);
        return true;
    }

    public void EndShadowPass()
    {
        if (!_shadowPass || _pointShadowPass)
            return;

        _shadowPass = false;
        _depthShader.Unbind();
        _shadowMap!.Unbind();
    }

    public void SetPointLight(Vector3 position, Vector3 color, float strength, float range)
    {
        _pointLightPosition = position;
        _pointLightColor = color * strength;
        _pointLightRange = range > 0.1f ? range : 25f;
    }

    public bool BeginPointShadowPass()
    {
        _pointShadowPass = false;
        _pointShadowsEnabled = !_wireframe && _pointLightColor.LengthSquared() > 1e-10f;
        if (!_pointShadowsEnabled || !EnsurePointDepthShader())
            return false;

        EnsurePointShadowMap();
        ComputePointShadowMatrices();
        _shadowPass = true;
        _pointShadowPass = true;
        _pointShadowMap!.Bind();
        rendererApi.SetDepthTest(true);
        _pointDepthShader!.Bind();
        _pointDepthShader.SetFloat3("u_LightPos", _pointLightPosition);
        _pointDepthShader.SetFloat("u_FarPlane", _pointLightRange);
        return true;
    }

    public void SetPointShadowFace(int face)
    {
        if (!_pointShadowPass)
            return;

        _pointShadowMap!.BindDepthCubemapFace(face);
        rendererApi.Clear();
        _pointDepthShader!.SetMat4("u_ShadowMatrix", _pointShadowMatrices[face]);
    }

    public void EndPointShadowPass()
    {
        if (!_pointShadowPass)
            return;

        _pointShadowPass = false;
        _shadowPass = false;
        _pointDepthShader!.Unbind();
        _pointShadowMap!.Unbind();
    }

    public void BeginTransparentPass() => rendererApi.SetDepthWrite(false);

    public void EndTransparentPass() => rendererApi.SetDepthWrite(true);

    public void SetEnvironment(string? resolvedHdrPath, float intensity)
    {
        _envIntensity = intensity;
        _environmentMap = resolvedHdrPath is null ? null : environmentMapFactory.GetOrCreate(resolvedHdrPath);
        // Generate the shared LUT here — doing it inside DrawMesh unbinds the mesh shader (GL_INVALID_OPERATION).
        if (_environmentMap is not null)
            _ = environmentMapFactory.GetBrdfLut();
    }

    private void ApplyCamera(Matrix4x4 view, Matrix4x4 projection, Vector3 viewPosition)
    {
        _view = view;
        _projection = projection;
        _viewProjection = view * projection;
        _viewPosition = viewPosition;
        // Shadow pass runs after BeginScene; re-enabled by BeginShadowPass / BeginPointShadowPass.
        _shadowEnabled = false;
        _pointShadowsEnabled = false;

        _cubeShader.Bind();
        _cubeShader.SetMat4(ViewProjectionUniform, _viewProjection);

        _texturedShader.Bind();
        _texturedShader.SetMat4(ViewProjectionUniform, _viewProjection);
        _texturedShader.SetFloat3("u_ViewPosition", viewPosition);

        if (_wireframeShader is not null)
        {
            _wireframeShader.Bind();
            _wireframeShader.SetMat4(ViewProjectionUniform, _viewProjection);
        }
    }

    private void DrawWireframe(Mesh mesh, Matrix4x4 transform, int entityId, Matrix4x4[]? bonePalette = null)
    {
        _wireframeShader!.Bind();
        _wireframeShader.SetMat4(ViewProjectionUniform, _viewProjection);
        _wireframeShader.SetMat4("u_Model", transform);
        UploadBones(_wireframeShader, bonePalette);
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

    private void BindCommon(IShader shader, Matrix4x4 transform, Vector4 color, int entityId, Matrix4x4[]? bonePalette = null)
    {
        BindIblAndPointShadowCubes();
        shader.Bind();
        shader.SetMat4("u_Model", transform);
        shader.SetMat4("u_NormalMatrix", ComputeNormalMatrix(transform));
        UploadBones(shader, bonePalette);
        shader.SetFloat4("u_Color", color);
        shader.SetInt("u_EntityID", entityId);
        shader.SetFloat3("lightColor", _ambientColor);
        shader.SetFloat("strength", _ambientStrength);
        shader.SetFloat3("u_LightDirection", _lightDirection);
        shader.SetFloat3("u_LightColor", _lightColor * _lightStrength);
        shader.SetMat4("u_LightSpaceMatrix", _lightSpaceMatrix);
        shader.SetInt("u_ShadowsEnabled", _shadowEnabled ? 1 : 0);
        if (_shadowEnabled)
            rendererApi.BindTexture2D(_shadowMap!.GetDepthAttachmentRendererId(), ShadowMapSlot);
        shader.SetFloat3("u_PointLightPosition", _pointLightPosition);
        shader.SetFloat3("u_PointLightColor", _pointLightColor);
        shader.SetFloat("u_PointLightRange", _pointLightRange);
        shader.SetInt("u_PointShadowsEnabled", _pointShadowsEnabled ? 1 : 0);
        if (_pointShadowsEnabled)
            rendererApi.BindTextureCube(_pointShadowMap!.GetDepthAttachmentRendererId(), PointShadowMapSlot);
        else
            environmentMapFactory.GetBlackCubemap().Bind(PointShadowMapSlot);
    }

    private void BindIblAndPointShadowCubes()
    {
        var black = environmentMapFactory.GetBlackCubemap();
        if (_environmentMap is not null)
        {
            _environmentMap.Irradiance.Bind(3);
            _environmentMap.Prefiltered.Bind(4);
        }
        else
        {
            black.Bind(3);
            black.Bind(4);
        }

        if (_pointShadowsEnabled)
            rendererApi.BindTextureCube(_pointShadowMap!.GetDepthAttachmentRendererId(), PointShadowMapSlot);
        else
            black.Bind(PointShadowMapSlot);
    }

    private void DrawShadowCaster(
        Mesh mesh,
        Matrix4x4 transform,
        MeshMaterial? material = null,
        Vector4 baseColor = default,
        Matrix4x4[]? bonePalette = null,
        Texture2D? albedoOverride = null)
    {
        var shader = _pointShadowPass ? _pointDepthShader! : _depthShader;
        shader.SetMat4("u_Model", transform);
        UploadBones(shader, bonePalette);

        var albedoMap = albedoOverride ?? material?.AlbedoTexture;
        if (!_pointShadowPass && material is not null && material.AlphaMode == MaterialAlphaMode.Mask)
        {
            shader.SetInt("u_HasAlbedoMap", albedoMap != null ? 1 : 0);
            shader.SetInt("u_AlphaMode", (int)material.AlphaMode);
            shader.SetFloat("u_AlphaCutoff", material.AlphaCutoff);
            shader.SetFloat4("u_Color", baseColor == default ? Vector4.One : baseColor);
            (albedoMap ?? textureFactory.GetWhiteTexture()).Bind(0);
        }
        else
        {
            shader.SetInt("u_AlphaMode", 0);
        }

        mesh.Bind();
        rendererApi.DrawIndexed(mesh.GetVertexArray(), (uint)mesh.GetIndexCount());
        _stats.DrawCalls++;
    }

    private static void UploadBones(IShader shader, Matrix4x4[]? bonePalette)
    {
        if (bonePalette is null)
        {
            shader.SetInt("u_Skinned", 0);
            return;
        }

        shader.SetInt("u_Skinned", 1);
        shader.SetMat4Array("u_BoneMatrices", bonePalette);
    }

    private void EnsureShadowMap()
    {
        if (_shadowMap is not null)
            return;

        _shadowMap = frameBufferFactory.Create(new FrameBufferSpecification(ShadowMapSize, ShadowMapSize)
        {
            AttachmentsSpec = new FrameBufferAttachmentSpecification([
                new FrameBufferTextureSpecification(FrameBufferTextureFormat.DepthComponent)
                {
                    Filter = FrameBufferTextureFilter.Nearest,
                    Wrap = FrameBufferTextureWrap.ClampToBorder
                }
            ])
        });
    }

    private void EnsurePointShadowMap()
    {
        if (_pointShadowMap is not null)
            return;

        _pointShadowMap = frameBufferFactory.Create(new FrameBufferSpecification(PointShadowMapSize, PointShadowMapSize)
        {
            AttachmentsSpec = new FrameBufferAttachmentSpecification([
                new FrameBufferTextureSpecification(FrameBufferTextureFormat.DepthCubemap)
                {
                    Filter = FrameBufferTextureFilter.Nearest,
                    Wrap = FrameBufferTextureWrap.ClampToEdge
                }
            ])
        });
    }

    private bool EnsurePointDepthShader()
    {
        if (_pointDepthShader is not null)
            return true;

        try
        {
            _pointDepthShader = shaderFactory.Create(
                ResolveHostShader("pointShadowsDepth.vert"),
                ResolveHostShader("pointShadowsDepth.frag"));
            return true;
        }
        catch (Exception ex)
        {
            Serilog.Log.ForContext<Graphics3D>().Error(ex, "Failed to load point shadow depth shader");
            _pointDepthShader = null;
            _pointShadowsEnabled = false;
            return false;
        }
    }

    private void ComputePointShadowMatrices()
    {
        var lightPos = _pointLightPosition;
        var proj = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 2f, 1f, PointShadowNear, _pointLightRange);
        Vector3[] targets =
        [
            lightPos + Vector3.UnitX,
            lightPos - Vector3.UnitX,
            lightPos + Vector3.UnitY,
            lightPos - Vector3.UnitY,
            lightPos + Vector3.UnitZ,
            lightPos - Vector3.UnitZ
        ];
        Vector3[] ups =
        [
            -Vector3.UnitY,
            -Vector3.UnitY,
            Vector3.UnitZ,
            -Vector3.UnitZ,
            -Vector3.UnitY,
            -Vector3.UnitY
        ];

        for (var i = 0; i < 6; i++)
            _pointShadowMatrices[i] = Matrix4x4.CreateLookAt(lightPos, targets[i], ups[i]) * proj;
    }

    private Matrix4x4 ComputeLightSpaceMatrix()
    {
        var direction = _lightDirection.LengthSquared() < 1e-6f
            ? new Vector3(0, -1, 0)
            : Vector3.Normalize(_lightDirection);
        var up = MathF.Abs(Vector3.Dot(direction, Vector3.UnitY)) > 0.99f ? Vector3.UnitZ : Vector3.UnitY;

        if (!TryFitLightOrthoToCamera(direction, up, out var lightView, out var lightProjection))
        {
            var lightPos = _viewPosition - direction * ShadowMaxDistance;
            lightView = Matrix4x4.CreateLookAt(lightPos, _viewPosition, up);
            lightProjection = Matrix4x4.CreateOrthographicOffCenter(
                -ShadowMaxDistance, ShadowMaxDistance, -ShadowMaxDistance, ShadowMaxDistance,
                ShadowNearPad, ShadowMaxDistance * 2f);
        }

        return lightView * lightProjection;
    }

    private bool TryFitLightOrthoToCamera(
        Vector3 direction, Vector3 up, out Matrix4x4 lightView, out Matrix4x4 lightProjection)
    {
        lightView = default;
        lightProjection = default;
        if (!Matrix4x4.Invert(_viewProjection, out var invViewProj))
            return false;

        Span<Vector3> corners = stackalloc Vector3[8];
        for (var c = 0; c < 8; c++)
        {
            var n = ClipCorners[c];
            var clip = Vector4.Transform(new Vector4(n, 1f), invViewProj);
            var w = MathF.Abs(clip.W) < 1e-6f ? 1e-6f : clip.W;
            corners[c] = new Vector3(clip.X / w, clip.Y / w, clip.Z / w);
        }

        for (var c = 4; c < 8; c++)
        {
            var toFar = corners[c] - _viewPosition;
            var dist = toFar.Length();
            if (dist > ShadowMaxDistance)
                corners[c] = _viewPosition + toFar * (ShadowMaxDistance / dist);
        }

        var center = Vector3.Zero;
        foreach (var corner in corners)
            center += corner;
        center /= 8f;

        var radius = 1f;
        foreach (var corner in corners)
            radius = MathF.Max(radius, Vector3.Distance(center, corner));

        var lightPos = center - direction * (radius + ShadowNearPad);
        lightView = Matrix4x4.CreateLookAt(lightPos, center, up);

        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        foreach (var corner in corners)
        {
            var ls = Vector3.Transform(corner, lightView);
            min = Vector3.Min(min, ls);
            max = Vector3.Max(max, ls);
        }

        const float pad = 1f;
        var near = MathF.Max(ShadowNearPad, -max.Z - pad);
        var far = MathF.Max(near + 1f, -min.Z + pad);
        lightProjection = Matrix4x4.CreateOrthographicOffCenter(
            min.X - pad, max.X + pad, min.Y - pad, max.Y + pad, near, far);
        return true;
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
        _skyboxShader?.Dispose();
        _skyboxShader = null!;
        _skyboxVao?.Dispose();
        _skyboxVao = null!;
        _depthShader?.Dispose();
        _depthShader = null!;
        _pointDepthShader?.Dispose();
        _pointDepthShader = null;
        _wireframeShader?.Dispose();
        _wireframeShader = null;

        _cubeMesh = null!;
        _shadowMap?.Dispose();
        _shadowMap = null;
        _pointShadowMap?.Dispose();
        _pointShadowMap = null;

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
