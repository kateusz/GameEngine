using System.Numerics;
using Engine.Platform.OpenGL;
using Engine.Platform.SilkNet;
using Engine.Renderer.Cameras;
using Engine.Renderer.Materials;
using Engine.Renderer.Shaders;
using Engine.Scene.Components;
using Serilog;
using Silk.NET.OpenGL;

namespace Engine.Renderer;

internal sealed class Graphics3D(IRendererAPI rendererApi, IShaderFactory shaderFactory) : IGraphics3D
{
    private static readonly ILogger Logger = Log.ForContext<Graphics3D>();

    private IShader _pbrShader = null!;
    private IShader _shadowShader = null!;
    private IShader _phongShader = null!;
    private IShadowMap? _shadowMap;

    // Default sun light properties (used when no directional light entity in scene)
    private Vector3 _lightPosition = new(-2.0f, 5.0f, 3.0f);
    private Vector3 _lightColor = new(1.0f, 0.95f, 0.9f);
    private float _shininess = 32.0f;

    // Current frame state
    private Matrix4x4 _lightSpaceMatrix = Matrix4x4.Identity;
    private Matrix4x4 _currentViewProjection = Matrix4x4.Identity;
    private Vector3 _currentViewPosition = Vector3.Zero;
    private uint _viewportWidth;
    private uint _viewportHeight;

    private readonly Statistics _stats = new();
    private IBLPrecomputer? _iblPrecomputer;
    private bool _disposed;

    private const uint ShadowMapSize = 4096;

    // PBR texture slot assignments
    private const int AlbedoSlot = 0;
    private const int NormalSlot = 1;
    private const int MetallicSlot = 2;
    private const int RoughnessSlot = 3;
    private const int AOSlot = 4;
    private const int EmissiveSlot = 5;
    private const int ShadowSlot = 6;
    private const int IrradianceSlot = 7;
    private const int PrefilterSlot = 8;
    private const int BrdfLutSlot = 9;

    public void Init()
    {
        _pbrShader = shaderFactory.Create("assets/shaders/opengl/pbr.vert", "assets/shaders/opengl/pbr.frag");
        _shadowShader = shaderFactory.Create("assets/shaders/opengl/shadow_depth.vert", "assets/shaders/opengl/shadow_depth.frag");
        _phongShader = shaderFactory.Create("assets/shaders/opengl/phong.vert", "assets/shaders/opengl/phong.frag");

        _shadowMap = new OpenGLShadowMap(ShadowMapSize, ShadowMapSize);

        // Enable seamless cubemap filtering for IBL (GL_TEXTURE_CUBE_MAP_SEAMLESS = 0x884F)
        SilkNetContext.GL.Enable((EnableCap)0x884F);

        // Set sampler uniforms once
        _pbrShader.Bind();
        _pbrShader.SetInt("u_AlbedoMap", AlbedoSlot);
        _pbrShader.SetInt("u_NormalMap", NormalSlot);
        _pbrShader.SetInt("u_MetallicMap", MetallicSlot);
        _pbrShader.SetInt("u_RoughnessMap", RoughnessSlot);
        _pbrShader.SetInt("u_AOMap", AOSlot);
        _pbrShader.SetInt("u_EmissiveMap", EmissiveSlot);
        _pbrShader.SetInt("u_ShadowMap", ShadowSlot);
        _pbrShader.SetInt("u_IrradianceMap", IrradianceSlot);
        _pbrShader.SetInt("u_PrefilterMap", PrefilterSlot);
        _pbrShader.SetInt("u_BrdfLUT", BrdfLutSlot);
        _pbrShader.Unbind();

        Logger.Information("PBR Graphics3D initialized with {ShadowMapSize}x{ShadowMapSize} shadow map", ShadowMapSize, ShadowMapSize);
    }

    public void SetEnvironmentMap(string hdrPath)
    {
        _iblPrecomputer?.Dispose();
        _iblPrecomputer = new IBLPrecomputer();
        _iblPrecomputer.Compute(hdrPath, shaderFactory);
    }

    // ============== Shadow Pass ==============

    public void BeginShadowPass(Matrix4x4 lightSpaceMatrix)
    {
        _lightSpaceMatrix = lightSpaceMatrix;

        // Enable 3D rendering state
        rendererApi.EnableDepthTest(true);
        rendererApi.EnableBlending(false);

        _shadowMap?.Bind();

        // Disable face culling for shadow pass - many imported models have single-sided geometry
        rendererApi.EnableFaceCulling(false);

        _shadowShader.Bind();
        _shadowShader.SetMat4("u_LightSpaceMatrix", lightSpaceMatrix);
    }

    public void DrawShadowMesh(Matrix4x4 transform, Mesh mesh)
    {
        _shadowShader.SetMat4("u_Model", transform);
        mesh.GetVertexArray().Bind();
        rendererApi.DrawIndexed(mesh.GetVertexArray(), (uint)mesh.GetIndexCount());
        _stats.DrawCalls++;
    }

    public void EndShadowPass()
    {
        _shadowShader.Unbind();

        // Restore back-face culling for main pass
        rendererApi.SetCullFace(true);
        rendererApi.EnableFaceCulling(true);

        _shadowMap?.Unbind();
    }

    // ============== Main PBR Pass ==============

    public void BeginScene(Camera camera, Matrix4x4 transform)
    {
        // Ensure 3D state is set (may not have gone through shadow pass)
        rendererApi.EnableDepthTest(true);
        rendererApi.EnableFaceCulling(true);

        _ = Matrix4x4.Invert(transform, out var transformInverted);

        // Row-vector convention: View * Projection (shaders use pos * Model * VP)
        var viewProj = transformInverted * camera.GetProjectionMatrix();

        _currentViewProjection = viewProj;
        _currentViewPosition = new Vector3(transform.M41, transform.M42, transform.M43);

        _pbrShader.Bind();
        _pbrShader.SetMat4("u_ViewProjection", viewProj);
        _pbrShader.SetFloat3("u_ViewPosition", _currentViewPosition);
        _pbrShader.SetMat4("u_LightSpaceMatrix", _lightSpaceMatrix);

        // Bind shadow map
        if (_shadowMap != null)
        {
            rendererApi.BindTextureUnit(ShadowSlot, _shadowMap.DepthTextureId);
            _pbrShader.SetInt("u_HasShadowMap", 1);
        }
        else
        {
            _pbrShader.SetInt("u_HasShadowMap", 0);
        }

        // Scene lighting controls
        _pbrShader.SetFloat("u_Exposure", 2.0f);
        _pbrShader.SetFloat("u_AmbientIntensity", 0.5f);
        _pbrShader.SetFloat3("u_AmbientColor", new Vector3(1.0f, 1.0f, 1.0f));

        // Bind IBL textures if available
        if (_iblPrecomputer is { IsReady: true })
        {
            _iblPrecomputer.BindIrradiance(IrradianceSlot);
            _iblPrecomputer.BindPrefilter(PrefilterSlot);
            _iblPrecomputer.BindBrdfLut(BrdfLutSlot);
            _pbrShader.SetInt("u_HasIBL", 1);
            _pbrShader.SetFloat("u_IBLIntensity", 0.5f);
        }
        else
        {
            _pbrShader.SetInt("u_HasIBL", 0);
        }

        // Default sun light (used when no directional light entity in scene)
        _pbrShader.SetInt("u_HasDirLight", 1);
        _pbrShader.SetFloat3("u_DirLightDirection", Vector3.Normalize(_lightPosition * -1));
        _pbrShader.SetFloat3("u_DirLightColor", _lightColor);
        _pbrShader.SetFloat("u_DirLightIntensity", 2.0f);

        _pbrShader.SetInt("u_NumPointLights", 0);
        _pbrShader.SetInt("u_NumSpotLights", 0);
    }

    public void SetLights(
        LightComponent? directionalLight, Vector3 dirLightDirection,
        ReadOnlySpan<(Vector3 Position, LightComponent Light)> pointLights,
        ReadOnlySpan<(Vector3 Position, LightComponent Light)> spotLights)
    {
        // Directional light - if scene has one, use it; otherwise keep default from BeginScene
        if (directionalLight != null)
        {
            _pbrShader.SetInt("u_HasDirLight", 1);
            _pbrShader.SetFloat3("u_DirLightDirection", dirLightDirection);
            _pbrShader.SetFloat3("u_DirLightColor", directionalLight.Color);
            _pbrShader.SetFloat("u_DirLightIntensity", directionalLight.Intensity);
        }

        // Point lights
        var numPoint = System.Math.Min(pointLights.Length, RenderingConstants.MaxPointLights);
        _pbrShader.SetInt("u_NumPointLights", numPoint);
        for (var i = 0; i < numPoint; i++)
        {
            var (pos, light) = pointLights[i];
            _pbrShader.SetFloat3($"u_PointLightPositions[{i}]", pos);
            _pbrShader.SetFloat3($"u_PointLightColors[{i}]", light.Color);
            _pbrShader.SetFloat($"u_PointLightIntensities[{i}]", light.Intensity);
            _pbrShader.SetFloat($"u_PointLightRanges[{i}]", light.Range);
        }

        // Spot lights
        var numSpot = System.Math.Min(spotLights.Length, RenderingConstants.MaxSpotLights);
        _pbrShader.SetInt("u_NumSpotLights", numSpot);
        for (var i = 0; i < numSpot; i++)
        {
            var (pos, light) = spotLights[i];
            _pbrShader.SetFloat3($"u_SpotLightPositions[{i}]", pos);
            _pbrShader.SetFloat3($"u_SpotLightDirections[{i}]", light.Direction);
            _pbrShader.SetFloat3($"u_SpotLightColors[{i}]", light.Color);
            _pbrShader.SetFloat($"u_SpotLightIntensities[{i}]", light.Intensity);
            _pbrShader.SetFloat($"u_SpotLightRanges[{i}]", light.Range);
            _pbrShader.SetFloat($"u_SpotLightInnerCones[{i}]", MathF.Cos(light.InnerConeAngle * MathF.PI / 180f));
            _pbrShader.SetFloat($"u_SpotLightOuterCones[{i}]", MathF.Cos(light.OuterConeAngle * MathF.PI / 180f));
        }
    }

    public void EndScene()
    {
        _pbrShader.Unbind();

        // Restore state for 2D/ImGui rendering
        rendererApi.EnableDepthTest(false);
        rendererApi.EnableFaceCulling(false);
        rendererApi.EnableBlending(true);
    }

    public void DrawMesh(Matrix4x4 transform, Mesh mesh, Vector4 color, int entityId = -1)
    {
        _pbrShader.Bind();
        _pbrShader.SetMat4("u_Model", transform);

        // Normal matrix: Inverse(Model) for row-vector convention (shader: normal * normalMatrix)
        if (!Matrix4x4.Invert(transform, out var normalMatrix))
        {
            normalMatrix = Matrix4x4.Identity;
        }
        _pbrShader.SetMat4("u_NormalMatrix", normalMatrix);

        // Bind PBR material
        var material = mesh.Material;
        if (material != null)
        {
            BindPBRMaterial(material);
        }
        else
        {
            // Fallback: use diffuse texture as albedo
            SetDefaultMaterial(color, mesh);
        }

        mesh.GetVertexArray().Bind();
        rendererApi.DrawIndexed(mesh.GetVertexArray(), (uint)mesh.GetIndexCount());
        _stats.DrawCalls++;
    }

    public void DrawModel(Matrix4x4 transform, MeshComponent meshComponent, ModelRendererComponent modelRenderer, int entityId = -1)
    {
        var mesh = meshComponent.Mesh;
        var color = modelRenderer.Color;

        var originalTexture = mesh.DiffuseTexture;
        if (modelRenderer.OverrideTexture != null)
        {
            mesh.DiffuseTexture = modelRenderer.OverrideTexture;
        }

        DrawMesh(transform, mesh, color, entityId);

        if (modelRenderer.OverrideTexture != null)
        {
            mesh.DiffuseTexture = originalTexture;
        }
    }

    private void BindPBRMaterial(PBRMaterial material)
    {
        // Albedo
        if (material.AlbedoMap != null)
        {
            material.AlbedoMap.Bind(AlbedoSlot);
            _pbrShader.SetInt("u_HasAlbedoMap", 1);
        }
        else
        {
            _pbrShader.SetInt("u_HasAlbedoMap", 0);
        }
        _pbrShader.SetFloat4("u_AlbedoColor", material.AlbedoColor);

        // Normal
        if (material.NormalMap != null)
        {
            material.NormalMap.Bind(NormalSlot);
            _pbrShader.SetInt("u_HasNormalMap", 1);
        }
        else
        {
            _pbrShader.SetInt("u_HasNormalMap", 0);
        }

        // Metallic
        if (material.MetallicMap != null)
        {
            material.MetallicMap.Bind(MetallicSlot);
            _pbrShader.SetInt("u_HasMetallicMap", 1);
        }
        else
        {
            _pbrShader.SetInt("u_HasMetallicMap", 0);
        }
        _pbrShader.SetFloat("u_Metallic", material.Metallic);

        // Roughness
        if (material.RoughnessMap != null)
        {
            material.RoughnessMap.Bind(RoughnessSlot);
            _pbrShader.SetInt("u_HasRoughnessMap", 1);
        }
        else
        {
            _pbrShader.SetInt("u_HasRoughnessMap", 0);
        }
        _pbrShader.SetFloat("u_Roughness", material.Roughness);

        // AO
        if (material.AmbientOcclusionMap != null)
        {
            material.AmbientOcclusionMap.Bind(AOSlot);
            _pbrShader.SetInt("u_HasAOMap", 1);
        }
        else
        {
            _pbrShader.SetInt("u_HasAOMap", 0);
        }
        _pbrShader.SetFloat("u_AO", material.AmbientOcclusion);

        // Emissive
        if (material.EmissiveMap != null)
        {
            material.EmissiveMap.Bind(EmissiveSlot);
            _pbrShader.SetInt("u_HasEmissiveMap", 1);
        }
        else
        {
            _pbrShader.SetInt("u_HasEmissiveMap", 0);
        }
        _pbrShader.SetFloat3("u_EmissiveColor", material.EmissiveColor);
        _pbrShader.SetFloat("u_EmissiveIntensity", material.EmissiveIntensity);
    }

    private void SetDefaultMaterial(Vector4 color, Mesh mesh)
    {
        var hasTexture = mesh.DiffuseTexture?.Width > 1;
        if (hasTexture == true)
        {
            mesh.DiffuseTexture.Bind(AlbedoSlot);
            _pbrShader.SetInt("u_HasAlbedoMap", 1);
        }
        else
        {
            _pbrShader.SetInt("u_HasAlbedoMap", 0);
        }

        _pbrShader.SetFloat4("u_AlbedoColor", color);
        _pbrShader.SetInt("u_HasNormalMap", 0);
        _pbrShader.SetInt("u_HasMetallicMap", 0);
        _pbrShader.SetFloat("u_Metallic", 0.0f);
        _pbrShader.SetInt("u_HasRoughnessMap", 0);
        _pbrShader.SetFloat("u_Roughness", 0.5f);
        _pbrShader.SetInt("u_HasAOMap", 0);
        _pbrShader.SetFloat("u_AO", 1.0f);
        _pbrShader.SetInt("u_HasEmissiveMap", 0);
        _pbrShader.SetFloat("u_EmissiveIntensity", 0.0f);
    }

    // Legacy light property setters
    public void SetLightPosition(Vector3 position) => _lightPosition = position;
    public void SetLightColor(Vector3 color) => _lightColor = color;
    public void SetShininess(float shininess) => _shininess = shininess;

    public void SetViewportSize(uint width, uint height)
    {
        _viewportWidth = width;
        _viewportHeight = height;
    }

    public void ResetStats() => _stats.DrawCalls = 0;
    public Statistics GetStats() => _stats;

    public void SetClearColor(Vector4 color) => rendererApi.SetClearColor(color);
    public void Clear() => rendererApi.Clear();

    /// <summary>
    /// Computes a light-space matrix for directional shadow mapping.
    /// </summary>
    public static Matrix4x4 ComputeDirectionalLightSpaceMatrix(Vector3 lightDirection, Vector3 sceneCenter, float sceneRadius)
    {
        var lightDir = Vector3.Normalize(lightDirection);
        var lightPos = sceneCenter - lightDir * sceneRadius * 2f;

        var lightView = Matrix4x4.CreateLookAt(lightPos, sceneCenter, Vector3.UnitY);
        var lightProjection = Matrix4x4.CreateOrthographic(
            sceneRadius * 2f, sceneRadius * 2f,
            0.1f, sceneRadius * 4f);

        return lightView * lightProjection;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _pbrShader?.Dispose();
        _shadowShader?.Dispose();
        _phongShader?.Dispose();
        _shadowMap?.Dispose();
        _iblPrecomputer?.Dispose();

        _disposed = true;
    }
}
