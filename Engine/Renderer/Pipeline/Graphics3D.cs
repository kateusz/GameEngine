using System.Numerics;
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
    
    private static readonly string[] PointPositionUniforms = Names("u_PointLightPositions");
    private static readonly string[] PointColorUniforms = Names("u_PointLightColors");
    private static readonly string[] PointIntensityUniforms = Names("u_PointLightIntensities");
    private static readonly string[] PointRangeUniforms = Names("u_PointLightRanges");
    
    private IShader _cubeShader = null!;
    private IShader _modelShader = null!;
    private IShader _depthShader = null!;
    private Mesh _cubeMesh = null!;
    private IFrameBuffer? _shadowMap;
    private bool _shadowPass;
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

    private readonly Statistics _stats = new();
    private bool _disposed;

    public void Init()
    {
        _cubeShader = shaderFactory.Create(ShaderId.Cube);
        _modelShader = shaderFactory.Create(ShaderId.Model);
        _depthShader = shaderFactory.Create(ShaderId.Depth);
        _cubeMesh = meshFactory.CreateCube();

        _cubeShader.Bind();
        _cubeShader.SetInt("u_ShadowMap", ShadowMapSlot);
        _cubeShader.Unbind();

        _modelShader.Bind();
        _modelShader.SetInt("u_DiffuseMap", 0);
        _modelShader.SetInt("u_SpecularMap", 1);
        _modelShader.SetInt("u_NormalMap", 2);
        _modelShader.SetInt("u_ShadowMap", ShadowMapSlot);
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
        float tilingFactor = 1.0f)
    {
        if (_shadowPass)
        {
            DrawShadow(_cubeMesh, transform);
            return;
        }

        rendererApi.SetDepthTest(true);
        BindCommon(_cubeShader, transform, color, entityId);
        
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
        _cubeShader.Unbind();
    }

    public void DrawMesh(Matrix4x4 transform, Mesh mesh, Vector4 tint, int entityId = -1)
    {
        if (_shadowPass)
        {
            DrawShadow(mesh, transform);
            return;
        }

        rendererApi.SetDepthTest(true);
        BindCommon(_modelShader, transform, tint, entityId);

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
        _modelShader.Unbind();
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
        shader.Unbind();
    }

    private void DrawShadow(Mesh mesh, Matrix4x4 transform)
    {
        rendererApi.SetDepthTest(true);
        _depthShader.SetMat4("u_Model", transform);
        mesh.Bind();
        rendererApi.DrawIndexed(mesh.GetVertexArray(), (uint)mesh.GetIndexCount());
        _stats.DrawCalls++;
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

        // Factory owns shader and cube-mesh lifetime; just release our references
        _cubeShader = null!;
        _modelShader = null!;
        _depthShader = null!;
        _cubeMesh = null!;

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}