using System.Numerics;
using Engine.Project;
using Engine.Renderer.Meshes;
using Engine.Renderer.Shaders;
using Engine.Renderer.Textures;

namespace Engine.Renderer.Pipeline;

/// <summary>Owns HDRI capture, IBL bake, and sky draw resources. Graphics3D only binds/draws.</summary>
internal sealed class EnvironmentLighting(IRendererAPI rendererApi, IShaderFactory shaderFactory) : IDisposable
{
    private const string ViewProjectionUniform = "u_ViewProjection";
    // ponytail: scale 4 puts a face at 2 units. PerspectiveNear >= 2 clips the sky.
    private const float SkyScale = 4f;

    private IShader _equirectShader = null!;
    private IShader _skyShader = null!;
    private IShader _irradianceShader = null!;
    private IShader _prefilterShader = null!;
    private IShader _brdfShader = null!;
    private Mesh _cubeMesh = null!;
    private uint _skyCubemap;
    private uint _irradiance;
    private uint _prefilter;
    private uint _brdfLut;
    private string _skySource = "";
    private bool _disposed;

    public bool HasIbl => _irradiance != 0 && _prefilter != 0 && _brdfLut != 0;
    public uint Irradiance => _irradiance;
    public uint Prefilter => _prefilter;
    public uint BrdfLut => _brdfLut;

    public void Init(Mesh cubeMesh)
    {
        _cubeMesh = cubeMesh;
        _equirectShader = shaderFactory.Create(ShaderId.EquirectToCube);
        _skyShader = shaderFactory.Create(ShaderId.Skybox);
        _irradianceShader = shaderFactory.Create(ShaderId.Irradiance);
        _prefilterShader = shaderFactory.Create(ShaderId.Prefilter);
        _brdfShader = shaderFactory.Create(ShaderId.BrdfLut);

        _equirectShader.Bind();
        _equirectShader.SetInt("u_Equirect", 0);
        _equirectShader.Unbind();
        _skyShader.Bind();
        _skyShader.SetInt("u_Skybox", 0);
        _skyShader.Unbind();
        _irradianceShader.Bind();
        _irradianceShader.SetInt("environmentMap", 0);
        _irradianceShader.Unbind();
        _prefilterShader.Bind();
        _prefilterShader.SetInt("environmentMap", 0);
        _prefilterShader.Unbind();
        _brdfShader.Bind();
        rendererApi.TryCreateBrdfLut(out _brdfLut);
        _brdfShader.Unbind();
    }

    public void SetSkybox(string? path)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (string.IsNullOrWhiteSpace(path))
        {
            ClearSky();
            return;
        }

        if (string.Equals(path, _skySource, StringComparison.Ordinal))
            return;

        var full = Path.IsPathRooted(path) ? Path.GetFullPath(path) : PathBuilder.Resolve(path);
        if (!rendererApi.TryCreateSkyCapture(full, out var environment, out var capture))
        {
            _skySource = path;
            return;
        }

        using (capture)
        {
            Span<Matrix4x4> faces = stackalloc Matrix4x4[LightingMath.PointShadowFaceCount];
            if (!LightingMath.TryBuildPointShadowFaces(Vector3.Zero, LightingMath.SkyCaptureFar, faces)
                || !DrawEnvironment(capture, environment, faces)
                || !capture.GenerateEnvironmentMips()
                || !DrawIrradiance(capture, faces)
                || !DrawPrefilter(capture, faces))
            {
                rendererApi.DeleteTexture(environment);
                rendererApi.DeleteTexture(capture.IrradianceId);
                rendererApi.DeleteTexture(capture.PrefilterId);
                _skySource = path;
                return;
            }

            ClearSky();
            _skyCubemap = environment;
            _irradiance = capture.IrradianceId;
            _prefilter = capture.PrefilterId;
            _skySource = path;
        }
    }

    public void DrawSkybox(Matrix4x4 skyViewProjection)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_skyCubemap == 0)
            return;

        rendererApi.SetDepthTest(true);
        rendererApi.SetDepthWrite(false);
        rendererApi.SetFaceCulling(false);
        try
        {
            _skyShader.Bind();
            _skyShader.SetMat4(ViewProjectionUniform, skyViewProjection);
            _skyShader.SetFloat("u_Scale", SkyScale);
            rendererApi.BindTextureCube(_skyCubemap, 0);
            _cubeMesh.Bind();
            rendererApi.DrawIndexed(_cubeMesh.GetVertexArray(), (uint)_cubeMesh.GetIndexCount());
        }
        finally
        {
            rendererApi.BindTextureCube(0, 0);
            _skyShader.Unbind();
            rendererApi.SetFaceCulling(true);
            rendererApi.SetDepthWrite(true);
        }
    }

    private bool DrawEnvironment(ISkyCapture capture, uint environment, ReadOnlySpan<Matrix4x4> faces) =>
        DrawFaces(_equirectShader, capture, environment, faces, mip: 0, LightingMath.SkyCaptureFaceSize, roughness: null);

    private bool DrawIrradiance(ISkyCapture capture, ReadOnlySpan<Matrix4x4> faces) =>
        DrawFaces(_irradianceShader, capture, capture.IrradianceId, faces, mip: 0, LightingMath.IrradianceFaceSize, roughness: null);

    private bool DrawPrefilter(ISkyCapture capture, ReadOnlySpan<Matrix4x4> faces)
    {
        for (var mip = 0; mip < LightingMath.PrefilterMipCount; mip++)
        {
            var roughness = mip / (float)(LightingMath.PrefilterMipCount - 1);
            var size = LightingMath.PrefilterFaceSize >> mip;
            if (!DrawFaces(_prefilterShader, capture, capture.PrefilterId, faces, mip, size, roughness))
                return false;
        }

        return true;
    }

    private bool DrawFaces(
        IShader shader, ISkyCapture capture, uint cubemap, ReadOnlySpan<Matrix4x4> faces,
        int mip, int size, float? roughness)
    {
        rendererApi.SetDepthTest(true);
        rendererApi.SetDepthWrite(true);
        rendererApi.SetFaceCulling(false);
        rendererApi.SetBlend(false);
        try
        {
            shader.Bind();
            shader.SetFloat("u_Scale", 1f);
            if (roughness is { } value)
                shader.SetFloat("u_Roughness", value);
            _cubeMesh.Bind();
            for (var i = 0; i < faces.Length; i++)
            {
                if (!capture.Begin(cubemap, i, mip, size))
                    return false;

                shader.SetMat4(ViewProjectionUniform, faces[i]);
                rendererApi.DrawIndexed(_cubeMesh.GetVertexArray(), (uint)_cubeMesh.GetIndexCount());
            }

            return true;
        }
        finally
        {
            shader.Unbind();
            rendererApi.SetFaceCulling(true);
            rendererApi.SetDepthWrite(true);
            rendererApi.SetBlend(true);
        }
    }

    private void ClearSky()
    {
        // Keep in sync with Graphics3D IBL texture slots.
        rendererApi.BindTextureCube(0, 13);
        rendererApi.BindTextureCube(0, 14);
        if (_skyCubemap != 0)
            rendererApi.DeleteTexture(_skyCubemap);
        if (_irradiance != 0)
            rendererApi.DeleteTexture(_irradiance);
        if (_prefilter != 0)
            rendererApi.DeleteTexture(_prefilter);
        _skyCubemap = 0;
        _irradiance = 0;
        _prefilter = 0;
        _skySource = "";
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        ClearSky();
        if (_brdfLut != 0)
            rendererApi.DeleteTexture(_brdfLut);
        _brdfLut = 0;
        _equirectShader = null!;
        _skyShader = null!;
        _irradianceShader = null!;
        _prefilterShader = null!;
        _brdfShader = null!;
        _cubeMesh = null!;
    }
}
