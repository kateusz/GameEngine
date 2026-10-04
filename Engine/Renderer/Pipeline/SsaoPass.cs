using System.Numerics;
using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Shaders;
using Engine.Renderer.Textures;
using Serilog;

namespace Engine.Renderer.Pipeline;

public sealed class SsaoPass(
    IRendererAPI rendererApi,
    IShaderFactory shaderFactory,
    IVertexArrayFactory vertexArrayFactory,
    IFrameBufferFactory frameBuffers,
    ITextureFactory textures) : IDisposable
{
    private static readonly ILogger Logger = Log.ForContext<SsaoPass>();

    private IShader? _shader;
    private IShader? _blur;
    private IVertexArray? _triangle;
    private IFrameBuffer? _geometry;
    private IFrameBuffer? _raw;
    private IFrameBuffer? _blurred;
    private Texture2D? _noise;
    private bool _initAttempted;
    private bool _available;
    private bool _invertWarned;
    private bool _resizeWarned;
    private bool _disposed;

    public bool Available
    {
        get
        {
            Init();
            return _available;
        }
    }

    public bool TryOcclude(
        uint width,
        uint height,
        Matrix4x4 projection,
        float radius,
        Action drawOpaque,
        out uint occlusionTexture)
    {
        occlusionTexture = 0;
        if (width == 0 || height == 0 || !Available || _shader == null || _blur == null || _triangle == null || _noise == null)
            return false;

        if (!Matrix4x4.Invert(projection, out var inverse))
        {
            if (!_invertWarned)
            {
                _invertWarned = true;
                Logger.Warning("SSAO skipped: the camera projection has no inverse");
            }

            return false;
        }

        if (!Ensure(width, height))
            return false;

        rendererApi.SetClearColor(Vector4.Zero);
        rendererApi.SetDepthTest(true);
        rendererApi.SetDepthWrite(true);
        _geometry!.Bind();
        try
        {
            rendererApi.Clear();
            drawOpaque();
        }
        finally
        {
            _geometry.Unbind();
        }

        DrawFullScreen(_shader, width, height, _raw!, () =>
        {
            rendererApi.BindTexture2D(_geometry.GetColorAttachmentRendererId(), 0);
            rendererApi.BindTexture2D(_geometry.GetDepthAttachmentRendererId(), 1);
            rendererApi.BindTexture2D(_noise.GetRendererId(), 2);
            _shader.SetMat4("u_Projection", projection);
            _shader.SetMat4("u_InverseProjection", inverse);
            _shader.SetFloat("u_Radius", radius);
            _shader.SetFloat("u_Bias", LightingMath.SsaoBias);
            _shader.SetFloat2("u_NoiseScale", new Vector2(width / 4f, height / 4f));
        });

        DrawFullScreen(_blur, width, height, _blurred!, () =>
            rendererApi.BindTexture2D(_raw!.GetColorAttachmentRendererId()));

        occlusionTexture = _blurred.GetColorAttachmentRendererId();
        return true;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _triangle?.Dispose();
        _geometry?.Dispose();
        _raw?.Dispose();
        _blurred?.Dispose();
        _noise?.Dispose();
        _available = false;
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private void DrawFullScreen(IShader shader, uint width, uint height, IFrameBuffer dest, Action bind)
    {
        rendererApi.SetDepthTest(false);
        rendererApi.SetBlend(false);
        rendererApi.SetFaceCulling(false);
        dest.Bind();
        try
        {
            shader.Bind();
            bind();
            rendererApi.DrawArrays(_triangle!, 3);
            shader.Unbind();
        }
        finally
        {
            dest.Unbind();
            rendererApi.SetDepthTest(true);
            rendererApi.SetBlend(true);
            rendererApi.SetFaceCulling(true);
        }
    }

    private void Init()
    {
        if (_initAttempted)
            return;

        _initAttempted = true;
        try
        {
            if (rendererApi.MaxFragmentTextureImageUnits < 16)
                throw new InvalidOperationException("SSAO needs 16 fragment texture image units");

            _shader = shaderFactory.Create(ShaderId.Ssao);
            _blur = shaderFactory.Create(ShaderId.SsaoBlur);
            _triangle = vertexArrayFactory.Create();
            _noise = textures.CreateFromRgba(PackNoise(LightingMath.CreateSsaoNoise()), 4, 4);
            var kernel = LightingMath.CreateSsaoKernel();
            _shader.Bind();
            _shader.SetInt("u_Normal", 0);
            _shader.SetInt("u_Depth", 1);
            _shader.SetInt("u_Noise", 2);
            for (var i = 0; i < kernel.Length; i++)
                _shader.SetFloat3($"u_Samples[{i}]", kernel[i]);
            _shader.Unbind();
            _blur.Bind();
            _blur.SetInt("u_Occlusion", 0);
            _blur.Unbind();
            _available = true;
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "SSAO disabled: failed to create shader or noise");
            _triangle?.Dispose();
            _triangle = null;
            _shader = null;
            _blur = null;
            _noise?.Dispose();
            _noise = null;
            _available = false;
        }
    }

    private bool Ensure(uint width, uint height)
    {
        try
        {
            _geometry = Fit(_geometry, width, height, GeometryTarget);
            _raw = Fit(_raw, width, height, OcclusionTarget);
            _blurred = Fit(_blurred, width, height, OcclusionTarget);
            return true;
        }
        catch (Exception ex)
        {
            if (!_resizeWarned)
            {
                _resizeWarned = true;
                Logger.Warning(ex, "SSAO target failed");
            }

            return false;
        }
    }

    private IFrameBuffer Fit(
        IFrameBuffer? target,
        uint width,
        uint height,
        Func<uint, uint, FrameBufferSpecification> spec)
    {
        if (target == null)
            return frameBuffers.Create(spec(width, height));

        var current = target.GetSpecification();
        if (current.Width != width || current.Height != height)
            target.Resize(width, height);
        return target;
    }

    private static byte[] PackNoise(Vector4[] noise)
    {
        var bytes = new byte[noise.Length * 4];
        for (var i = 0; i < noise.Length; i++)
        {
            bytes[i * 4] = ToByte(noise[i].X);
            bytes[i * 4 + 1] = ToByte(noise[i].Y);
            bytes[i * 4 + 2] = ToByte(noise[i].Z);
            bytes[i * 4 + 3] = 255;
        }

        return bytes;
    }

    private static byte ToByte(float channel) =>
        (byte)System.Math.Round(System.Math.Clamp(channel * 0.5f + 0.5f, 0f, 1f) * 255f);

    private static FrameBufferSpecification GeometryTarget(uint width, uint height) =>
        new(width, height)
        {
            AttachmentsSpec = new FrameBufferAttachmentSpecification([
                new FrameBufferTextureSpecification(FrameBufferTextureFormat.RGBA16F)
                {
                    Filter = FrameBufferTextureFilter.Nearest,
                    Wrap = FrameBufferTextureWrap.ClampToEdge
                },
                new FrameBufferTextureSpecification(FrameBufferTextureFormat.Depth)
            ])
        };

    private static FrameBufferSpecification OcclusionTarget(uint width, uint height) =>
        new(width, height)
        {
            AttachmentsSpec = new FrameBufferAttachmentSpecification([
                new FrameBufferTextureSpecification(FrameBufferTextureFormat.RGBA8)
                {
                    Filter = FrameBufferTextureFilter.Nearest,
                    Wrap = FrameBufferTextureWrap.ClampToEdge
                }
            ])
        };
}
