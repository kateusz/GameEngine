using System.Numerics;
using System.Runtime.InteropServices;
using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Renderer.Meshes;
using Engine.Renderer.Shaders;
using Engine.Renderer.Textures;

namespace Engine.Renderer.Pipeline;

internal sealed class Graphics3D(
    IRendererAPI rendererApi,
    IShaderFactory shaderFactory,
    IMeshFactory meshFactory,
    ITextureFactory textureFactory,
    IFrameBufferFactory frameBuffers) : IGraphics3D
{
    private const string ViewProjectionUniform = "u_ViewProjection";
    private const int ShadowMapSlot = 3;
    private const int PointShadowSlot = 4;
    private const int OcclusionMapSlot = 12;
    
    private static readonly string[] PointPositionUniforms = Names("u_PointLightPositions");
    private static readonly string[] PointColorUniforms = Names("u_PointLightColors");
    private static readonly string[] PointIntensityUniforms = Names("u_PointLightIntensities");
    private static readonly string[] PointRangeUniforms = Names("u_PointLightRanges");
    private static readonly string[] PointShadowEnabledUniforms = Names("u_PointShadowsEnabled");
    private static readonly string[] PointShadowMapUniforms = Names("u_PointShadowMaps");
    
    private IShader _cubeShader = null!;
    private IShader _modelShader = null!;
    private IShader _depthShader = null!;
    private IShader _pointDepthShader = null!;
    private Mesh _cubeMesh = null!;
    private IFrameBuffer? _shadowMap;
    private readonly Dictionary<int, IFrameBuffer> _pointShadowMapsByEntity = new();
    private readonly int[] _pointShadowEntity = new int[LightingMath.MaxPointLights];
    private readonly bool[] _pointShadowEnabled = new bool[LightingMath.MaxPointLights];
    private bool _shadowPass;
    private bool _pointShadowPass;
    private IFrameBuffer? _activePointMap;
    private Matrix4x4 _lightViewProjection = Matrix4x4.Identity;
    private bool _shadowsEnabled;

    private Matrix4x4 _viewProjection = Matrix4x4.Identity;
    private Vector3 _viewPosition;
    private Vector3 _ambientColor = Vector3.One;
    private float _ambientStrength = 0.1f;
    private Vector3 _lightDirection = new(0, -1, 0);
    private Vector3 _lightColor = Vector3.Zero;
    
    private readonly PointLightData[] _pointLights = new PointLightData[LightingMath.MaxPointLights];
    private int _pointLightCount;

    private readonly List<MeshInstanceData> _instanceUpload = [];
    private readonly Statistics _stats = new();
    private bool _disposed;

    public void Init()
    {
        _cubeShader = shaderFactory.Create(ShaderId.Cube);
        _modelShader = shaderFactory.Create(ShaderId.Model);
        _depthShader = shaderFactory.Create(ShaderId.Depth);
        _pointDepthShader = shaderFactory.Create(ShaderId.PointDepth);
        _cubeMesh = meshFactory.CreateCube();

        _cubeShader.Bind();
        _cubeShader.SetInt("u_ShadowMap", ShadowMapSlot);
        for (var i = 0; i < LightingMath.MaxPointLights; i++)
            _cubeShader.SetInt(PointShadowMapUniforms[i], PointShadowSlot + i);
        _cubeShader.Unbind();

        _modelShader.Bind();
        _modelShader.SetInt("u_DiffuseMap", 0);
        _modelShader.SetInt("u_MetallicRoughnessMap", 1);
        _modelShader.SetInt("u_NormalMap", 2);
        _modelShader.SetInt("u_ShadowMap", ShadowMapSlot);
        _modelShader.SetInt("u_OcclusionMap", OcclusionMapSlot);
        for (var i = 0; i < LightingMath.MaxPointLights; i++)
            _modelShader.SetInt(PointShadowMapUniforms[i], PointShadowSlot + i);
        _modelShader.Unbind();
    }

    public void SetDirectionalShadow(Matrix4x4 lightViewProjection, bool enabled)
    {
        _lightViewProjection = lightViewProjection;
        _shadowsEnabled = enabled;
    }

    public void BeginShadowPass(Matrix4x4 lightViewProjection)
    {
        _shadowPass = true;
        var map = ShadowMap();
        map.Bind();
        rendererApi.SetDepthTest(true);
        rendererApi.SetDepthWrite(true);
        rendererApi.Clear();
        _depthShader.Bind();
        _depthShader.SetMat4(ViewProjectionUniform, lightViewProjection);
    }

    public void EndShadowPass()
    {
        _depthShader.Unbind();
        _shadowMap?.Unbind();
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
            map = PointShadowMap(entityId);
        }
        catch (Exception)
        {
            _pointShadowEnabled[lightIndex] = false;
            return false;
        }

        _pointShadowEntity[lightIndex] = entityId;
        _shadowPass = true;
        _pointShadowPass = true;
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
        if (!_pointShadowMapsByEntity.TryGetValue(entityId, out _))
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
        _pointShadowPass = false;
        _shadowPass = false;
    }

    public void BeginScene(in SceneView view)
    {
        _viewProjection = view.ViewProjection;
        _viewPosition = view.ViewPosition;
        UploadFrame(_cubeShader);
        UploadFrame(_modelShader);
    }

    public void EndScene()
    {
    }

    public void DrawCube(Matrix4x4 transform, Vector4 color, int entityId = -1, Texture2D? texture = null,
        float tilingFactor = 1.0f, float metallic = 0f, float roughness = 0.5f, float ao = 1f)
    {
        if (_shadowPass)
        {
            DrawShadow(_cubeMesh, transform);
            return;
        }

        rendererApi.SetDepthTest(true);
        BindCommon(_cubeShader, transform, color, entityId);
        
        _cubeShader.SetFloat("u_TilingFactor", tilingFactor);
        _cubeShader.SetFloat("u_Metallic", metallic);
        _cubeShader.SetFloat("u_Roughness", roughness);
        _cubeShader.SetFloat("u_Ao", ao);
        _cubeShader.SetInt("u_UseTexture", texture != null ? 1 : 0);
        if (texture != null)
        {
            texture.Bind(0);
            _cubeShader.SetInt("u_Texture", 0);
        }

        _cubeMesh.Bind();
        rendererApi.DrawIndexed(_cubeMesh.GetVertexArray(), (uint)_cubeMesh.GetIndexCount());
        _stats.DrawCalls++;
        _cubeShader.Unbind();
    }

    public void DrawMesh(Matrix4x4 transform, Mesh mesh, Vector4 tint, int entityId = -1,
        float metallic = 0f, float roughness = 0.5f, float ao = 1f)
    {
        Span<MeshDrawInstance> one = stackalloc MeshDrawInstance[1];
        one[0] = new MeshDrawInstance
        {
            Transform = transform,
            EntityId = entityId,
            Tint = tint,
            Metallic = metallic,
            Roughness = roughness,
            Ao = ao
        };
        DrawMeshInstances(mesh, one);
    }

    public void DrawMeshInstances(Mesh mesh, ReadOnlySpan<MeshDrawInstance> instances)
    {
        if (instances.IsEmpty)
            return;

        if (_shadowPass)
        {
            UploadAndDraw(mesh, instances, _depthShader);
            return;
        }

        rendererApi.SetDepthTest(true);
        var first = instances[0];
        _modelShader.Bind();
        _modelShader.SetFloat4("u_Color", first.Tint);
        _modelShader.SetFloat("u_Metallic", first.Metallic);
        _modelShader.SetFloat("u_Roughness", first.Roughness);
        _modelShader.SetFloat("u_Ao", first.Ao);
        _modelShader.SetFloat3("u_BaseColor", mesh.BaseColorFactor);
        _modelShader.SetInt("u_HasDiffuseMap", mesh.HasDiffuseMap ? 1 : 0);
        _modelShader.SetInt("u_HasMetallicRoughnessMap", mesh.HasMetallicRoughnessMap ? 1 : 0);
        _modelShader.SetInt("u_HasNormalMap", mesh.HasNormalMap ? 1 : 0);
        _modelShader.SetInt("u_HasOcclusionMap", mesh.HasOcclusionMap ? 1 : 0);

        (mesh.DiffuseTexture ?? textureFactory.GetWhiteTexture()).Bind(0);
        (mesh.MetallicRoughnessTexture ?? textureFactory.GetWhiteTexture()).Bind(1);
        (mesh.NormalTexture ?? textureFactory.GetFlatNormalTexture()).Bind(2);
        (mesh.OcclusionTexture ?? textureFactory.GetWhiteTexture()).Bind(OcclusionMapSlot);

        UploadAndDraw(mesh, instances, _modelShader);
        _modelShader.Unbind();
    }

    private void UploadAndDraw(Mesh mesh, ReadOnlySpan<MeshDrawInstance> instances, IShader shader)
    {
        mesh.Bind();
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

            rendererApi.DrawIndexed(mesh.GetVertexArray(), (uint)mesh.GetIndexCount());
            _stats.DrawCalls++;
            return;
        }

        shader.SetInt("u_Instanced", 1);
        _instanceUpload.Clear();
        foreach (var instance in instances)
            _instanceUpload.Add(PackInstance(instance));

        rendererApi.DrawIndexedInstanced(
            mesh.GetVertexArray(), (uint)mesh.GetIndexCount(), CollectionsMarshal.AsSpan(_instanceUpload));
        _stats.DrawCalls++;
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

    private void UploadFrame(IShader shader)
    {
        shader.Bind();
        shader.SetMat4(ViewProjectionUniform, _viewProjection);
        shader.SetFloat3("u_AmbientColor", _ambientColor);
        shader.SetFloat("u_AmbientStrength", _ambientStrength);
        shader.SetFloat3("u_LightDirection", _lightDirection);
        shader.SetFloat3("u_LightColor", _lightColor);
        shader.SetFloat3("u_ViewPosition", _viewPosition);
        shader.SetInt("u_PointLightCount", _pointLightCount);
        for (var i = 0; i < LightingMath.MaxPointLights; i++)
        {
            shader.SetFloat3(PointPositionUniforms[i], _pointLights[i].Position);
            shader.SetFloat3(PointColorUniforms[i], _pointLights[i].Color);
            shader.SetFloat(PointIntensityUniforms[i], _pointLights[i].Intensity);
            shader.SetFloat(PointRangeUniforms[i], _pointLights[i].Range);
        }
        shader.SetMat4("u_LightViewProjection", _lightViewProjection);
        shader.SetInt("u_ShadowsEnabled", _shadowsEnabled ? 1 : 0);
        if (_shadowsEnabled && _shadowMap != null)
            rendererApi.BindTexture2D(_shadowMap.GetDepthAttachmentRendererId(), ShadowMapSlot);
        for (var i = 0; i < LightingMath.MaxPointLights; i++)
        {
            shader.SetInt(PointShadowEnabledUniforms[i], _pointShadowEnabled[i] ? 1 : 0);
            if (_pointShadowEnabled[i] && _pointShadowEntity[i] >= 0
                && _pointShadowMapsByEntity.TryGetValue(_pointShadowEntity[i], out var pointMap))
                rendererApi.BindTextureCube(pointMap.GetDepthAttachmentRendererId(), PointShadowSlot + i);
        }
        shader.Unbind();
    }

    private void DrawShadow(Mesh mesh, Matrix4x4 transform)
    {
        var shader = _pointShadowPass ? _pointDepthShader : _depthShader;
        rendererApi.SetDepthTest(true);
        if (!_pointShadowPass)
            shader.SetInt("u_Instanced", 0);
        shader.SetMat4("u_Model", transform);
        mesh.Bind();
        rendererApi.DrawIndexed(mesh.GetVertexArray(), (uint)mesh.GetIndexCount());
        _stats.DrawCalls++;
    }

    private IFrameBuffer PointShadowMap(int entityId)
    {
        if (_pointShadowMapsByEntity.TryGetValue(entityId, out var existing))
            return existing;

        var size = (uint)LightingMath.PointShadowFaceResolution;
        var spec = new FrameBufferSpecification(size, size)
        {
            AttachmentsSpec = new FrameBufferAttachmentSpecification([
                new FrameBufferTextureSpecification(FrameBufferTextureFormat.DepthCubemap)
            ])
        };
        var map = frameBuffers.Create(spec);
        _pointShadowMapsByEntity[entityId] = map;
        return map;
    }

    private IFrameBuffer ShadowMap()
    {
        if (_shadowMap != null)
            return _shadowMap;

        var size = (uint)LightingMath.ShadowMapResolution;
        var spec = new FrameBufferSpecification(size, size)
        {
            AttachmentsSpec = new FrameBufferAttachmentSpecification([
                new FrameBufferTextureSpecification(FrameBufferTextureFormat.DepthComponent)
                {
                    Filter = FrameBufferTextureFilter.Nearest,
                    Wrap = FrameBufferTextureWrap.ClampToBorder
                }
            ])
        };
        _shadowMap = frameBuffers.Create(spec);
        return _shadowMap;
    }
    
    private static string[] Names(string uniform)
    {
        var names = new string[LightingMath.MaxPointLights];
        for (var i = 0; i < names.Length; i++)
            names[i] = $"{uniform}[{i}]";
        return names;
    }

    private static void BindCommon(IShader shader, Matrix4x4 transform, Vector4 color, int entityId)
    {
        shader.Bind();
        shader.SetMat4("u_Model", transform);
        shader.SetMat4("u_NormalMatrix", ComputeNormalMatrix(transform));
        shader.SetFloat4("u_Color", color);
        shader.SetInt("u_EntityID", entityId);
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

        _shadowMap?.Dispose();
        _shadowMap = null;
        foreach (var map in _pointShadowMapsByEntity.Values)
            map.Dispose();
        _pointShadowMapsByEntity.Clear();

        // Factory owns shader and cube-mesh lifetime; just release our references
        _cubeShader = null!;
        _modelShader = null!;
        _depthShader = null!;
        _pointDepthShader = null!;
        _cubeMesh = null!;

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}