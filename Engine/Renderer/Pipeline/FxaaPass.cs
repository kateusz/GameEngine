using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Shaders;
using Serilog;

namespace Engine.Renderer.Pipeline;

public sealed class FxaaPass(
    IRendererAPI rendererApi,
    IShaderFactory shaderFactory,
    IVertexArrayFactory vertexArrayFactory,
    IFrameBufferFactory frameBuffers) : IDisposable
{
    private static readonly ILogger Logger = Log.ForContext<FxaaPass>();

    private IShader? _shader;
    private IVertexArray? _triangle;
    private IFrameBuffer? _scene;
    private IFrameBuffer? _resolve;
    private bool _initAttempted;
    private bool _disposed;

    public bool Available { get; private set; }

    public void Init()
    {
        if (_initAttempted)
            return;

        _initAttempted = true;
        try
        {
            _shader = shaderFactory.Create(ShaderId.Fxaa);
            _triangle = vertexArrayFactory.Create();
            _shader.Bind();
            _shader.SetInt("u_Texture", 0);
            _shader.Unbind();
            Available = true;
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "FXAA disabled: failed to create shader or triangle");
            Available = false;
            _triangle?.Dispose();
            _triangle = null;
            _shader = null;
        }
    }

    public IFrameBuffer? DrawScene(uint width, uint height, Action draw)
    {
        if (width == 0 || height == 0 || !Ensure(ref _scene, width, height, depth: true))
            return null;

        _scene!.Bind();
        try
        {
            draw();
        }
        finally
        {
            _scene.Unbind();
        }

        return _scene;
    }

    public IFrameBuffer Resolve(IFrameBuffer source)
    {
        var spec = source.GetSpecification();
        if (!Ready() || !Ensure(ref _resolve, spec.Width, spec.Height, depth: false))
            return source;

        Apply(source.GetColorAttachmentRendererId(), spec.Width, spec.Height, _resolve);
        return _resolve!;
    }

    public void Apply(uint sourceTextureId, uint width, uint height, IFrameBuffer? dest)
    {
        if (!Ready() || width == 0 || height == 0)
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
            _shader!.Bind();
            rendererApi.BindTexture2D(sourceTextureId);
            rendererApi.SetBoundTexture2DFilterLinear();
            _shader.SetFloat("u_InverseWidth", 1f / width);
            _shader.SetFloat("u_InverseHeight", 1f / height);
            rendererApi.DrawArrays(_triangle!, 3);
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
        _scene?.Dispose();
        _scene = null;
        _resolve?.Dispose();
        _resolve = null;
        Available = false;
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private bool Ready()
    {
        Init();
        return Available && _shader != null && _triangle != null;
    }

    private bool Ensure(ref IFrameBuffer? target, uint width, uint height, bool depth)
    {
        try
        {
            if (target == null)
                target = frameBuffers.Create(Target(width, height, depth));
            else
            {
                var spec = target.GetSpecification();
                if (spec.Width != width || spec.Height != height)
                    target.Resize(width, height);
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "FXAA framebuffer failed");
            target?.Dispose();
            target = null;
            return false;
        }
    }

    private static FrameBufferSpecification Target(uint width, uint height, bool depth)
    {
        FrameBufferTextureSpecification color = new(depth
            ? FrameBufferTextureFormat.RGBA16F
            : FrameBufferTextureFormat.RGBA8)
        {
            Filter = FrameBufferTextureFilter.Linear,
            Wrap = FrameBufferTextureWrap.ClampToEdge
        };
        FrameBufferTextureSpecification[] attachments = depth
            ? [color, new FrameBufferTextureSpecification(FrameBufferTextureFormat.Depth)]
            : [color];
        return new FrameBufferSpecification(width, height)
        {
            AttachmentsSpec = new FrameBufferAttachmentSpecification(attachments)
        };
    }
}
