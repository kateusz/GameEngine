using System.Numerics;
using System.Runtime.InteropServices;
using Engine.Renderer.Buffers;
using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Renderer.Meshes;
using Engine.Renderer.Shaders;
using Engine.Renderer.Textures;

namespace Engine.Renderer.Pipeline;

internal sealed class Graphics3D(
    IRendererAPI rendererApi,
    IShaderFactory shaderFactory,
    IMeshFactory meshFactory,
    IFrameBufferFactory frameBuffers) : IGraphics3D
{
    private const string ViewProjectionUniform = "u_ViewProjection";
    private const int ShadowMapSlot = 3;
    private const int PointShadowSlot = 4;
    private const int OcclusionMapSlot = 12;
    private const int IrradianceSlot = 13;
    private const int PrefilterSlot = 14;
    private const int BrdfLutSlot = 15;
    private const uint FrameBinding = 0;

    // Keep in sync with the material flags in modelShader.frag.
    [Flags]
    private enum MaterialFlags
    {
        DiffuseMap = 1,
        MetallicRoughnessMap = 2,
        NormalMap = 4,
        ReconstructNormalZ = 8,
        OcclusionMap = 16,
        AlphaCutout = 32
    }

    private readonly record struct ModelMaterial(
        Vector4 Tint, Vector4 Factors, Vector3 Emissive, Vector3 BaseColor, MaterialFlags Flags);

    private static readonly string[] PointShadowMapUniforms = Names("u_PointShadowMaps");
    
    private IShader _cubeShader = null!;
    private IShader _modelShader = null!;
    private ModelMaterial? _modelMaterial;
    private IShader _emissiveShader = null!;
    private IShader _depthShader = null!;
    private IShader _pointDepthShader = null!;
    private IUniformBuffer _frame = null!;
    private Mesh _cubeMesh = null!;
    private readonly EnvironmentLighting _environment = new(rendererApi, shaderFactory);
    private ShadowMapStore? _boundShadowMaps;
    private ShadowMapStore? _fallbackShadowMaps;
    private readonly int[] _pointShadowEntity = new int[LightingMath.MaxPointLights];
    private readonly bool[] _pointShadowEnabled = new bool[LightingMath.MaxPointLights];
    private bool _shadowPass;
    private bool _pointShadowPass;
    private bool _colorPass;
    private IShader? _colorShader;
    private bool? _drawCulling;
    private IFrameBuffer? _activePointMap;
    private Matrix4x4 _lightViewProjection = Matrix4x4.Identity;
    private Matrix4x4 _farLightViewProjection = Matrix4x4.Identity;
    private bool _shadowsEnabled;
    private ShadowQuality _quality = new(DirectionalResolution: 1024);

    private Matrix4x4 _viewProjection = Matrix4x4.Identity;
    private Vector3 _viewPosition;
    private Vector3 _ambientColor = Vector3.One;
    private float _ambientStrength = 0.1f;
    private Vector3 _lightDirection = new(0, -1, 0);
    private Vector3 _lightColor = Vector3.Zero;
    
    private readonly PointLightData[] _pointLights = new PointLightData[LightingMath.MaxPointLights];
    private int _pointLightCount;

    private readonly List<MeshInstanceData> _instanceUpload = [];
    private Statistics _stats = new();
    private bool _disposed;
    private object? _sceneIdentity;

    private ShadowMapStore ShadowMaps =>
        _boundShadowMaps ?? (_fallbackShadowMaps ??= new ShadowMapStore(frameBuffers));

    public void Init()
    {
        _cubeShader = shaderFactory.Create(ShaderId.Cube);
        _modelShader = shaderFactory.Create(ShaderId.Model);
        _modelMaterial = null;
        _depthShader = shaderFactory.Create(ShaderId.Depth);
        _pointDepthShader = shaderFactory.Create(ShaderId.PointDepth);
        _cubeMesh = meshFactory.CreateCube();
        _frame = rendererApi.CreateUniformBuffer(FrameUniforms.ByteSize);

        _cubeShader.Bind();
        _cubeShader.SetInt("u_Texture", 0);
        _cubeShader.SetInt("u_ShadowMap", ShadowMapSlot);
        _cubeShader.SetInt("u_Irradiance", IrradianceSlot);
        _cubeShader.SetInt("u_Prefilter", PrefilterSlot);
        _cubeShader.SetInt("u_BrdfLut", BrdfLutSlot);
        for (var i = 0; i < LightingMath.MaxPointLights; i++)
            _cubeShader.SetInt(PointShadowMapUniforms[i], PointShadowSlot + i);
        _cubeShader.Unbind();

        _depthShader.Bind();
        _depthShader.SetInt("u_DiffuseMap", 0);
        _depthShader.SetFloat2("u_DiffuseUvScale", Vector2.One);
        _depthShader.Unbind();
        _pointDepthShader.Bind();
        _pointDepthShader.SetInt("u_DiffuseMap", 0);
        _pointDepthShader.SetFloat2("u_DiffuseUvScale", Vector2.One);
        _pointDepthShader.Unbind();
        _modelShader.Bind();
        _modelShader.SetInt("u_DiffuseMap", 0);
        _modelShader.SetInt("u_MetallicRoughnessMap", 1);
        _modelShader.SetInt("u_NormalMap", 2);
        _modelShader.SetInt("u_ShadowMap", ShadowMapSlot);
        _modelShader.SetInt("u_OcclusionMap", OcclusionMapSlot);
        _modelShader.SetInt("u_Irradiance", IrradianceSlot);
        _modelShader.SetInt("u_Prefilter", PrefilterSlot);
        _modelShader.SetInt("u_BrdfLut", BrdfLutSlot);
        for (var i = 0; i < LightingMath.MaxPointLights; i++)
            _modelShader.SetInt(PointShadowMapUniforms[i], PointShadowSlot + i);
        _modelShader.SetFloat2("u_DiffuseUvScale", Vector2.One);
        _modelShader.SetFloat2("u_MetallicRoughnessUvScale", Vector2.One);
        _modelShader.SetFloat2("u_NormalUvScale", Vector2.One);
        _modelShader.SetFloat2("u_OcclusionUvScale", Vector2.One);
        _modelShader.Unbind();

        _emissiveShader = shaderFactory.Create(ShaderId.Emissive);
        _emissiveShader.Bind();
        _emissiveShader.SetInt("u_EmissiveMap", 0);
        _emissiveShader.SetFloat2("u_EmissiveUvScale", Vector2.One);
        _emissiveShader.Unbind();

        _environment.Init(_cubeMesh);
    }

    public ShadowQuality ShadowQuality => _quality;

    public void BindShadowMapStore(ShadowMapStore? store)
    {
        _boundShadowMaps = store;
        // Quality may have changed while no store was bound (unit recorders, editor preferences).
        store?.SyncQuality(_quality);
    }

    public void ReleaseSceneResources()
    {
        if (_shadowPass) throw new InvalidOperationException("Cannot retire scene resources during a shadow pass.");
        // Host-owned bound store is cleared by ShadowRenderer; only retire the fallback.
        _fallbackShadowMaps?.Clear();
        Array.Clear(_pointShadowEnabled);
        Array.Fill(_pointShadowEntity, -1);
        _shadowsEnabled = false;
        _sceneIdentity = null;
    }

    public bool ActivateScene(object identity)
    {
        if (ReferenceEquals(_sceneIdentity, identity)) return false;
        ReleaseSceneResources();
        _sceneIdentity = identity;
        return true;
    }

    public void SetShadowQuality(ShadowQuality quality)
    {
        _quality = quality.Sanitized();
        _boundShadowMaps?.SyncQuality(_quality);
        _fallbackShadowMaps?.SyncQuality(_quality);
    }

    public void SetFarDirectionalShadow(Matrix4x4 lightViewProjection) =>
        _farLightViewProjection = lightViewProjection;

    public void SetDirectionalShadow(Matrix4x4 lightViewProjection, bool enabled)
    {
        _lightViewProjection = lightViewProjection;
        _shadowsEnabled = enabled;
    }

    public void BeginShadowPass(Matrix4x4 lightViewProjection) =>
        BeginShadowCascade(lightViewProjection, 0);

    public void BeginShadowCascade(Matrix4x4 lightViewProjection, int cascade)
    {
        _shadowPass = true;
        _colorShader = null;
        _drawCulling = null;
        var map = ShadowMaps.GetOrCreateDirectional(_quality);
        map.Bind();
        rendererApi.SetDepthTest(true);
        rendererApi.SetDepthWrite(true);
        var res = (uint)_quality.DirectionalResolution;
        rendererApi.SetCullFrontFaces(false);
        if (cascade == 0)
        {
            rendererApi.SetViewport(0, 0, res * (uint)_quality.Cascades, res);
            rendererApi.Clear();
        }

        rendererApi.SetViewport((int)(cascade * res), 0, res, res);
        _depthShader.Bind();
        _depthShader.SetMat4(ViewProjectionUniform, lightViewProjection);
    }

    public void EndShadowPass()
    {
        _depthShader.Unbind();
        RestoreDrawCulling();
        ShadowMaps.Directional?.Unbind();
        _shadowPass = false;
    }

    public bool BeginPointShadowFace(
        int lightIndex, int entityId, int face,
        Matrix4x4 viewProjection, Vector3 lightPosition, float range)
    {
        if ((uint)lightIndex >= LightingMath.MaxPointLights || (uint)face >= LightingMath.PointShadowFaceCount)
            return false;

        IFrameBuffer map;
        try
        {
            map = ShadowMaps.GetOrCreatePoint(entityId, _quality);
        }
        catch (Exception)
        {
            _pointShadowEnabled[lightIndex] = false;
            return false;
        }

        _pointShadowEntity[lightIndex] = entityId;
        _shadowPass = true;
        _pointShadowPass = true;
        _colorShader = null;
        _drawCulling = null;
        _activePointMap = map;
        map.Bind();
        map.BindDepthCubemapFace(face);
        rendererApi.SetDepthTest(true);
        rendererApi.SetDepthWrite(true);
        rendererApi.Clear();
        rendererApi.SetCullFrontFaces(true);

        _pointDepthShader.Bind();
        _pointDepthShader.SetMat4(ViewProjectionUniform, viewProjection);
        _pointDepthShader.SetFloat3("u_LightPosition", lightPosition);
        _pointDepthShader.SetFloat("u_LightRange", range);
        _pointShadowEnabled[lightIndex] = true;
        return true;
    }

    public bool UseCachedPointShadow(int lightIndex, int entityId)
    {
        if ((uint)lightIndex >= LightingMath.MaxPointLights)
            return false;
        if (!ShadowMaps.TryGetPoint(entityId, out _))
            return false;

        _pointShadowEntity[lightIndex] = entityId;
        _pointShadowEnabled[lightIndex] = true;
        return true;
    }

    public void EndPointShadowFace()
    {
        _pointDepthShader.Unbind();
        _activePointMap?.Unbind();
        _activePointMap = null;
        rendererApi.SetCullFrontFaces(false);
        rendererApi.SetFaceCulling(true);
        _drawCulling = null;
        _pointShadowPass = false;
        _shadowPass = false;
    }

    public void SetSkybox(string? path)
    {
        _environment.SetSkybox(path);
        _colorShader = null;
        _drawCulling = null;
    }

    public void DrawSkybox(Matrix4x4 skyViewProjection)
    {
        _environment.DrawSkybox(skyViewProjection);
        _colorShader = null;
        _drawCulling = null;
    }

    public void BeginScene(in SceneView view)
    {
        EndScene();
        _modelMaterial = null;
        _viewProjection = view.ViewProjection;
        _viewPosition = view.ViewPosition;
        UploadFrame();
        rendererApi.SetDepthTest(true);
        rendererApi.SetCullFrontFaces(false);
        _colorPass = true;
    }

    public void EndScene()
    {
        if (!_colorPass)
            return;

        _colorShader?.Unbind();
        _colorShader = null;
        RestoreDrawCulling();
        _colorPass = false;
    }

    public void DrawCube(Matrix4x4 transform, Vector4 color, int entityId = -1, Texture2D? texture = null,
        float tilingFactor = 1.0f, float metallic = 0f, float roughness = 0.5f, float ao = 1f,
        Vector3 emissive = default)
    {
        if (_shadowPass)
        {
            DrawShadow(_cubeMesh, transform, 0);
            return;
        }

        BeginColorDraw(_cubeShader);
        if (_colorPass)
            SetDrawCulling(true);

        _cubeShader.SetMat4("u_Model", transform);
        _cubeShader.SetMat4("u_NormalMatrix", ComputeNormalMatrix(transform));
        _cubeShader.SetFloat4("u_Color", color);
        _cubeShader.SetInt("u_EntityID", entityId);
        
        _cubeShader.SetFloat("u_TilingFactor", tilingFactor);
        _cubeShader.SetFloat("u_Metallic", metallic);
        _cubeShader.SetFloat("u_Roughness", roughness);
        _cubeShader.SetFloat("u_Ao", ao);
        _cubeShader.SetFloat3("u_Emissive", emissive);
        _cubeShader.SetInt("u_UseTexture", texture != null ? 1 : 0);
        if (texture != null)
        {
            _cubeShader.SetFloat2("u_TextureUvScale", texture.UvScale);
            texture.Bind(0);
        }

        _cubeMesh.Bind();
        rendererApi.DrawIndexed(_cubeMesh.GetVertexArray(), (uint)_cubeMesh.GetIndexCount());
        RecordDraw(_cubeMesh, instanceCount: 1, isCube: true);
        if (!_colorPass)
            _cubeShader.Unbind();
    }

    public void DrawMesh(Matrix4x4 transform, Mesh mesh, Vector4 tint, int entityId = -1,
        float metallic = 0f, float roughness = 0.5f, float ao = 1f, Vector3 emissive = default)
    {
        Span<MeshDrawInstance> one = stackalloc MeshDrawInstance[1];
        one[0] = new MeshDrawInstance
        {
            Transform = transform,
            EntityId = entityId,
            Tint = tint,
            Metallic = metallic,
            Roughness = roughness,
            Ao = ao,
            Emissive = emissive
        };
        DrawMeshInstances(mesh, one);
    }

    public void DrawMeshInstances(Mesh mesh, ReadOnlySpan<MeshDrawInstance> instances, int level = 0)
    {
        if (instances.IsEmpty)
            return;

        if (_shadowPass)
        {
            // Spec: point faces store linear dist/range via pointDepth; depth.frag is window Z.
            if (_pointShadowPass)
            {
                for (var i = 0; i < instances.Length; i++)
                    DrawShadow(mesh, instances[i].Transform, level);
                return;
            }

            ApplyShadowSurface(_depthShader, mesh);
            UploadAndDraw(mesh, instances, _depthShader, level);
            return;
        }

        BeginColorDraw(_modelShader);
        SetDrawCulling(!mesh.DoubleSided);
        UploadModelMaterial(mesh, instances[0]);
        BindModelTexture(mesh.DiffuseTexture, 0, "u_DiffuseUvScale");
        BindModelTexture(mesh.MetallicRoughnessTexture, 1, "u_MetallicRoughnessUvScale");
        BindModelTexture(mesh.NormalTexture, 2, "u_NormalUvScale");
        BindModelTexture(mesh.OcclusionTexture, OcclusionMapSlot, "u_OcclusionUvScale");

        UploadAndDraw(mesh, instances, _modelShader, level);
        if (!_colorPass)
        {
            _modelShader.Unbind();
            RestoreDrawCulling();
        }

        DrawEmissive(mesh, instances, level);
    }

    private void BeginColorDraw(IShader shader)
    {
        if (!_colorPass)
            rendererApi.SetDepthTest(true);

        BindColorShader(shader);
    }

    private void BindColorShader(IShader shader)
    {
        if (ReferenceEquals(_colorShader, shader))
            return;

        shader.Bind();
        if (_colorPass)
            _colorShader = shader;
    }

    private void SetDrawCulling(bool enabled)
    {
        if (_drawCulling == enabled)
            return;

        rendererApi.SetFaceCulling(enabled);
        _drawCulling = enabled;
    }

    private void RestoreDrawCulling()
    {
        if (_drawCulling == false)
            rendererApi.SetFaceCulling(true);

        _drawCulling = null;
    }

    private void UploadModelMaterial(Mesh mesh, in MeshDrawInstance instance)
    {
        var cutout = mesh.AlphaCutout && mesh.HasDiffuseMap;
        var flags = (mesh.HasDiffuseMap ? MaterialFlags.DiffuseMap : 0)
                    | (mesh.HasMetallicRoughnessMap ? MaterialFlags.MetallicRoughnessMap : 0)
                    | (mesh.HasNormalMap ? MaterialFlags.NormalMap : 0)
                    | (mesh.NormalTexture?.NormalTangentZReconstruct == true ? MaterialFlags.ReconstructNormalZ : 0)
                    | (mesh.HasOcclusionMap ? MaterialFlags.OcclusionMap : 0)
                    | (cutout ? MaterialFlags.AlphaCutout : 0);
        var material = new ModelMaterial(
            instance.Tint,
            new Vector4(instance.Metallic, instance.Roughness, instance.Ao, cutout ? mesh.AlphaCutoff : 0f),
            WithoutEmissiveMap(mesh, instance.Emissive),
            mesh.BaseColorFactor,
            flags);
        if (_modelMaterial == material)
            return;

        _modelShader.SetFloat4("u_Color", material.Tint);
        _modelShader.SetFloat4("u_MaterialFactors", material.Factors);
        _modelShader.SetFloat3("u_Emissive", material.Emissive);
        _modelShader.SetFloat3("u_BaseColor", material.BaseColor);
        _modelShader.SetInt("u_MaterialFlags", (int)material.Flags);
        _modelMaterial = material;
    }

    private void BindModelTexture(Texture2D? texture, int slot, string scaleUniform)
    {
        if (texture is null)
            return;

        _modelShader.SetFloat2(scaleUniform, texture.UvScale);
        texture.Bind(slot);
    }

    private void DrawEmissive(Mesh mesh, ReadOnlySpan<MeshDrawInstance> instances, int level)
    {
        var factor = mesh.EmissiveFactor;
        if (mesh.EmissiveTexture == null || (factor.X <= 0f && factor.Y <= 0f && factor.Z <= 0f))
            return;

        // ponytail: second pass on unit 0. The lit shader already uses 0–15.
        // Cutout holes can pick up this add; discard with the albedo alpha if that shows up.
        rendererApi.SetBlend(true);
        rendererApi.SetAdditiveBlend(true);
        rendererApi.SetDepthWrite(false);
        if (!_colorPass)
            rendererApi.SetFaceCulling(!mesh.DoubleSided);
        try
        {
            BindColorShader(_emissiveShader);
            _emissiveShader.SetFloat3("u_Emissive", factor);
            SetMapUvScale(_emissiveShader, "u_EmissiveUvScale", mesh.EmissiveTexture);
            mesh.EmissiveTexture.Bind(0);
            UploadAndDraw(mesh, instances, _emissiveShader, level);
        }
        finally
        {
            if (!_colorPass)
            {
                _emissiveShader.Unbind();
                rendererApi.SetFaceCulling(true);
            }
            rendererApi.SetDepthWrite(true);
            rendererApi.SetAdditiveBlend(false);
            rendererApi.SetBlend(false);
        }
    }

    private static Vector3 WithoutEmissiveMap(Mesh mesh, Vector3 combined)
    {
        if (mesh.EmissiveTexture == null)
            return combined;

        return new Vector3(
            MathF.Max(0f, combined.X - mesh.EmissiveFactor.X),
            MathF.Max(0f, combined.Y - mesh.EmissiveFactor.Y),
            MathF.Max(0f, combined.Z - mesh.EmissiveFactor.Z));
    }

    private void UploadAndDraw(Mesh mesh, ReadOnlySpan<MeshDrawInstance> instances, IShader shader, int level)
    {
        mesh.BindLevel(level);
        var indexCount = (uint)mesh.IndexCountAt(level);
        if (instances.Length == 1)
        {
            var instance = instances[0];
            shader.SetInt("u_Instanced", 0);
            shader.SetMat4("u_Model", instance.Transform);
            if (!_shadowPass)
            {
                shader.SetMat4("u_NormalMatrix", ComputeNormalMatrix(instance.Transform));
                shader.SetInt("u_EntityID", instance.EntityId);
            }

            rendererApi.DrawIndexed(mesh.GetVertexArray(), indexCount);
            RecordDraw(mesh, instanceCount: 1, isCube: false, level);
            return;
        }

        shader.SetInt("u_Instanced", 1);
        _instanceUpload.Clear();
        foreach (var instance in instances)
            _instanceUpload.Add(PackInstance(instance));

        rendererApi.DrawIndexedInstanced(
            mesh.GetVertexArray(), indexCount, CollectionsMarshal.AsSpan(_instanceUpload));
        RecordDraw(mesh, instances.Length, isCube: false, level);
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
    
    public void SetPointLights(ReadOnlySpan<PointLightData> lights)
    {
        _pointLightCount = System.Math.Min(lights.Length, LightingMath.MaxPointLights);
        lights[.._pointLightCount].CopyTo(_pointLights);
        Array.Clear(_pointLights, _pointLightCount, LightingMath.MaxPointLights - _pointLightCount);
        Array.Clear(_pointShadowEnabled);
        Array.Fill(_pointShadowEntity, -1);
    }

    private void UploadFrame()
    {
        var ibl = _environment.HasIbl;
        var frame = new FrameUniforms
        {
            ViewProjection = Matrix4x4.Transpose(_viewProjection),
            LightViewProjection = Matrix4x4.Transpose(_lightViewProjection),
            FarLightViewProjection = Matrix4x4.Transpose(_farLightViewProjection),
            AmbientColorStrength = new Vector4(_ambientColor, _ambientStrength),
            LightDirection = new Vector4(_lightDirection, 0f),
            LightColor = new Vector4(_lightColor, 0f),
            ViewPosition = new Vector4(_viewPosition, 0f),
            PointLightCount = _pointLightCount,
            ShadowsEnabled = _shadowsEnabled ? 1 : 0,
            CascadeCount = _quality.Cascades,
            ShadowPcf = _quality.PcfTaps,
            CascadeSplit = _quality.Distance * 0.25f,
            ShadowMapSize = _quality.DirectionalResolution,
            PointPcf = _quality.PointPcf ? _quality.PcfTaps : 1,
            PointMapSize = _quality.PointResolution,
            Ibl = ibl ? 1 : 0
        };
        for (var i = 0; i < LightingMath.MaxPointLights; i++)
        {
            frame.PointPositions[i] = new Vector4(_pointLights[i].Position, 0f);
            frame.PointColors[i] = new Vector4(_pointLights[i].Color, 0f);
            frame.PointIntensityRange[i] = new Vector4(_pointLights[i].Intensity, _pointLights[i].Range, 0f, 0f);
            if (_pointShadowEnabled[i])
                frame.PointShadowMask |= 1 << i;
        }

        _frame.SetData(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref frame, 1)));
        _frame.BindTo(FrameBinding);

        var maps = ShadowMaps;
        if (_shadowsEnabled && maps.Directional != null)
            rendererApi.BindTexture2D(maps.Directional.GetDepthAttachmentRendererId(), ShadowMapSlot);
        for (var i = 0; i < LightingMath.MaxPointLights; i++)
        {
            if (_pointShadowEnabled[i] && _pointShadowEntity[i] >= 0
                && maps.TryGetPoint(_pointShadowEntity[i], out var pointMap))
                rendererApi.BindTextureCube(pointMap.GetDepthAttachmentRendererId(), PointShadowSlot + i);
        }

        if (ibl)
        {
            rendererApi.BindTextureCube(_environment.Irradiance, IrradianceSlot);
            rendererApi.BindTextureCube(_environment.Prefilter, PrefilterSlot);
            rendererApi.BindTexture2D(_environment.BrdfLut, BrdfLutSlot);
        }
    }

    private void ApplyShadowSurface(IShader shader, Mesh mesh)
    {
        var cutout = mesh.AlphaCutout && mesh.HasDiffuseMap;
        shader.SetInt("u_AlphaTest", cutout ? 1 : 0);
        SetDrawCulling(!mesh.DoubleSided);
        if (cutout)
        {
            shader.SetFloat("u_AlphaCutoff", mesh.AlphaCutoff);
            SetMapUvScale(shader, "u_DiffuseUvScale", mesh.DiffuseTexture);
            mesh.DiffuseTexture!.Bind(0);
        }
    }

    private static void SetMapUvScale(IShader shader, string uniform, Texture2D? texture)
    {
        if (texture is not null)
            shader.SetFloat2(uniform, texture.UvScale);
    }

    private void DrawShadow(Mesh mesh, Matrix4x4 transform, int level)
    {
        var shader = _pointShadowPass ? _pointDepthShader : _depthShader;
        ApplyShadowSurface(shader, mesh);
        if (!_pointShadowPass)
            shader.SetInt("u_Instanced", 0);
        shader.SetMat4("u_Model", transform);
        mesh.BindLevel(level);
        rendererApi.DrawIndexed(mesh.GetVertexArray(), (uint)mesh.IndexCountAt(level));
        RecordDraw(mesh, instanceCount: 1, isCube: mesh == _cubeMesh, level);
    }

    private void RecordDraw(Mesh mesh, int instanceCount, bool isCube, int level = 0)
    {
        _stats.DrawCalls++;
        if (_pointShadowPass)
            _stats.PointShadowDrawCalls++;
        else if (_shadowPass)
            _stats.DirectionalShadowDrawCalls++;
        else
            _stats.ColorDrawCalls++;

        if (isCube)
            _stats.CubeDraws++;
        else
            _stats.MeshDraws++;

        if (instanceCount > 1)
            _stats.InstancedDraws++;

        _stats.Instances += (uint)instanceCount;
        _stats.Vertices += (long)mesh.VertexCount * instanceCount;
        var indexCount = isCube ? mesh.GetIndexCount() : mesh.IndexCountAt(level);
        _stats.Indices += (long)indexCount * instanceCount;
    }

    private static string[] Names(string uniform)
    {
        var names = new string[LightingMath.MaxPointLights];
        for (var i = 0; i < names.Length; i++)
            names[i] = $"{uniform}[{i}]";
        return names;
    }

    private static Matrix4x4 ComputeNormalMatrix(Matrix4x4 model) =>
        Matrix4x4.Invert(model, out var inv) ? Matrix4x4.Transpose(inv) : Matrix4x4.Identity;

    /// <summary>
    /// Attribute columns have no transpose flag, so the bytes are the transpose of the uniform upload.
    /// </summary>
    internal static MeshInstanceData PackInstance(MeshDrawInstance instance) => new()
    {
        Model = Matrix4x4.Transpose(instance.Transform),
        Normal = Matrix4x4.Transpose(ComputeNormalMatrix(instance.Transform)),
        EntityId = instance.EntityId
    };

    public void ResetStats() => _stats = new Statistics();

    public Statistics GetStats() => _stats;

    public void SetClearColor(Vector4 color) => rendererApi.SetClearColor(color);

    public void Clear() => rendererApi.Clear();

    public void Dispose()
    {
        if (_disposed)
            return;

        _fallbackShadowMaps?.Dispose();
        _fallbackShadowMaps = null;
        _boundShadowMaps = null;
        _frame?.Dispose();
        _frame = null!;
        _environment.Dispose();

        // Factory owns shader and cube-mesh lifetime; just release our references
        _colorShader = null;
        _cubeShader = null!;
        _modelShader = null!;
        _depthShader = null!;
        _pointDepthShader = null!;
        _cubeMesh = null!;

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
