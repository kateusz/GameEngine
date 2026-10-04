using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Shaders;
using Serilog;

namespace Engine.Renderer.Pipeline;

public sealed class TonemapPass(
    IRendererAPI rendererApi,
    IShaderFactory shaderFactory,
    IVertexArrayFactory vertexArrayFactory,
    IFrameBufferFactory frameBuffers) : IDisposable
{
    private static readonly ILogger Logger = Log.ForContext<TonemapPass>();

    private const int BlurPasses = 10;

    private IShader? _shader;
    private IShader? _extract;
    private IShader? _blur;
    private IVertexArray? _triangle;
    private IFrameBuffer? _output;
    private IFrameBuffer? _bright;
    private readonly IFrameBuffer?[] _pingPong = new IFrameBuffer?[2];
    private bool _initAttempted;
    private bool _available;
    private bool _targetWarned;
    private bool _disposed;

    public bool Available
    {
        get
        {
            Init();
            return _available;
        }
    }

    public IFrameBuffer? Resolve(IFrameBuffer scene)
    {
        var spec = scene.GetSpecification();
        if (!Available || spec.Width == 0 || spec.Height == 0 || !Ensure(spec.Width, spec.Height))
            return _output;

        Draw(scene.GetColorAttachmentRendererId(), spec.Width, spec.Height, _output);
        return _output;
    }

    public void Draw(uint sourceTextureId, uint width, uint height, IFrameBuffer? dest)
    {
        if (!Available || width == 0 || height == 0 || _shader == null || _triangle == null)
            return;

        rendererApi.SetDepthTest(false);
        rendererApi.SetBlend(false);
        rendererApi.SetFaceCulling(false);
        try
        {
            var bloomId = Bloom(sourceTextureId, width, height);
            if (dest != null)
                dest.Bind();
            else
                rendererApi.BindDefaultFramebuffer();

            rendererApi.SetViewport(0, 0, width, height);
            _shader.Bind();
            _shader.SetInt("u_BloomEnabled", bloomId != 0 ? 1 : 0);
            rendererApi.BindTexture2D(sourceTextureId);
            if (bloomId != 0)
                rendererApi.BindTexture2D(bloomId, 1);
            rendererApi.DrawArrays(_triangle, 3);
            _shader.Unbind();
        }
        finally
        {
            rendererApi.SetDepthTest(true);
            rendererApi.SetBlend(true);
            rendererApi.SetFaceCulling(true);
            dest?.Unbind();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _triangle?.Dispose();
        _triangle = null;
        _shader = null;
        _extract = null;
        _blur = null;
        _output?.Dispose();
        _output = null;
        DisposeBloomTargets();
        _available = false;
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private void Init()
    {
        if (_initAttempted)
            return;

        _initAttempted = true;
        try
        {
            _shader = shaderFactory.Create(ShaderId.Tonemap);
            _triangle = vertexArrayFactory.Create();
            _shader.Bind();
            _shader.SetInt("u_Color", 0);
            _shader.SetInt("u_Bloom", 1);
            _shader.Unbind();
            _available = true;
            try
            {
                _extract = shaderFactory.Create(ShaderId.BloomExtract);
                _blur = shaderFactory.Create(ShaderId.BloomBlur);
                if (_extract == null || _blur == null)
                    return;

                _extract.Bind();
                _extract.SetInt("u_Color", 0);
                _extract.Unbind();
                _blur.Bind();
                _blur.SetInt("u_Image", 0);
                _blur.Unbind();
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Bloom disabled: failed to create shader");
                _extract = null;
                _blur = null;
            }
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Tonemap disabled: failed to create shader or target");
            _triangle?.Dispose();
            _triangle = null;
            _shader = null;
            _output?.Dispose();
            _output = null;
            _available = false;
        }
    }

    private uint Bloom(uint sourceTextureId, uint width, uint height)
    {
        if (_extract == null || _blur == null || !EnsureBloom(width, height))
            return 0;

        Blit(_extract, sourceTextureId, width, height, _bright!, horizontal: null);
        var horizontal = true;
        var first = true;
        IFrameBuffer? last = null;
        for (var i = 0; i < BlurPasses; i++)
        {
            var destIndex = horizontal ? 1 : 0;
            var sourceId = first
                ? _bright!.GetColorAttachmentRendererId()
                : _pingPong[horizontal ? 0 : 1]!.GetColorAttachmentRendererId();
            last = _pingPong[destIndex];
            Blit(_blur, sourceId, width, height, last!, horizontal);
            horizontal = !horizontal;
            first = false;
        }

        return last?.GetColorAttachmentRendererId() ?? 0;
    }

    private void Blit(IShader shader, uint sourceTextureId, uint width, uint height, IFrameBuffer dest, bool? horizontal)
    {
        dest.Bind();
        rendererApi.SetViewport(0, 0, width, height);
        shader.Bind();
        if (horizontal is { } axis)
            shader.SetInt("u_Horizontal", axis ? 1 : 0);
        rendererApi.BindTexture2D(sourceTextureId);
        rendererApi.DrawArrays(_triangle!, 3);
        shader.Unbind();
        dest.Unbind();
    }

    private bool EnsureBloom(uint width, uint height)
    {
        try
        {
            _bright = Fit(_bright, width, height);
            _pingPong[0] = Fit(_pingPong[0], width, height);
            _pingPong[1] = Fit(_pingPong[1], width, height);
            return true;
        }
        catch (Exception ex)
        {
            if (!_targetWarned)
            {
                _targetWarned = true;
                Logger.Warning(ex, "Bloom target failed");
            }

            DisposeBloomTargets();
            return false;
        }
    }

    private IFrameBuffer Fit(IFrameBuffer? target, uint width, uint height)
    {
        if (target == null)
            return frameBuffers.Create(HdrTarget(width, height));

        var spec = target.GetSpecification();
        if (spec.Width != width || spec.Height != height)
            target.Resize(width, height);
        return target;
    }

    private void DisposeBloomTargets()
    {
        _bright?.Dispose();
        _bright = null;
        _pingPong[0]?.Dispose();
        _pingPong[0] = null;
        _pingPong[1]?.Dispose();
        _pingPong[1] = null;
    }

    private static FrameBufferSpecification HdrTarget(uint width, uint height) =>
        new(width, height)
        {
            AttachmentsSpec = new FrameBufferAttachmentSpecification([
                new FrameBufferTextureSpecification(FrameBufferTextureFormat.RGBA16F)
                {
                    Filter = FrameBufferTextureFilter.Linear,
                    Wrap = FrameBufferTextureWrap.ClampToEdge
                }
            ])
        };

    private bool Ensure(uint width, uint height)
    {
        try
        {
            if (_output == null)
                _output = frameBuffers.Create(DisplayTarget(width, height));
            else
            {
                var spec = _output.GetSpecification();
                if (spec.Width != width || spec.Height != height)
                    _output.Resize(width, height);
            }

            return true;
        }
        catch (Exception ex)
        {
            if (!_targetWarned)
            {
                _targetWarned = true;
                Logger.Warning(ex, "Tonemap target failed");
            }

            _output?.Dispose();
            _output = null;
            return false;
        }
    }

    private static FrameBufferSpecification DisplayTarget(uint width, uint height) =>
        new(width, height)
        {
            AttachmentsSpec = new FrameBufferAttachmentSpecification([
                new FrameBufferTextureSpecification(FrameBufferTextureFormat.RGBA8)
                {
                    Filter = FrameBufferTextureFilter.Linear,
                    Wrap = FrameBufferTextureWrap.ClampToEdge
                }
            ])
        };
}
