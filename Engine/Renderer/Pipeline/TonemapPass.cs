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

    private IShader? _shader;
    private IVertexArray? _triangle;
    private IFrameBuffer? _output;
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

        if (dest != null)
            dest.Bind();
        else
            rendererApi.BindDefaultFramebuffer();

        rendererApi.SetViewport(0, 0, width, height);
        rendererApi.SetDepthTest(false);
        rendererApi.SetBlend(false);
        rendererApi.SetFaceCulling(false);
        try
        {
            _shader.Bind();
            rendererApi.BindTexture2D(sourceTextureId);
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
        _output?.Dispose();
        _output = null;
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
            _shader.Unbind();
            _available = true;
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
