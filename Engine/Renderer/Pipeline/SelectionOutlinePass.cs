using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Shaders;
using Serilog;

namespace Engine.Renderer.Pipeline;

public sealed class SelectionOutlinePass(
    IRendererAPI rendererApi,
    IShaderFactory shaderFactory,
    IVertexArrayFactory vertexArrayFactory,
    IFrameBufferFactory frameBuffers) : IDisposable
{
    private static readonly ILogger Logger = Log.ForContext<SelectionOutlinePass>();

    private IShader? _shader;
    private IVertexArray? _triangle;
    private IFrameBuffer? _output;
    private bool _initAttempted;
    private bool _disposed;

    public bool Available { get; private set; }

    public IFrameBuffer Resolve(IFrameBuffer colorSource, IFrameBuffer scene, int entityId)
    {
        var spec = colorSource.GetSpecification();
        if (entityId <= 0 || spec.Width == 0 || spec.Height == 0 || !EnsureInitialized() || _output == null)
            return colorSource;

        var entityTexture = scene.GetColorAttachmentRendererId(1);
        if (entityTexture == 0 || !Fit(spec.Width, spec.Height))
            return colorSource;

        Draw(colorSource.GetColorAttachmentRendererId(), entityTexture, spec.Width, spec.Height, entityId);
        return _output;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _triangle?.Dispose();
        _triangle = null;
        _output?.Dispose();
        _output = null;
        _shader = null;
        Available = false;
        _disposed = true;
    }

    private bool EnsureInitialized()
    {
        if (_initAttempted)
            return Available && _shader != null && _triangle != null && _output != null;

        _initAttempted = true;
        try
        {
            _shader = shaderFactory.Create(ShaderId.SelectionOutline);
            _triangle = vertexArrayFactory.Create();
            _output = frameBuffers.Create(ColorTarget(1, 1));
            _shader.Bind();
            _shader.SetInt("u_Color", 0);
            _shader.SetInt("u_EntityIds", 1);
            _shader.Unbind();
            Available = true;
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Selection outline disabled: failed to create shader or buffer");
            Available = false;
            _triangle?.Dispose();
            _triangle = null;
            _output?.Dispose();
            _output = null;
            _shader = null;
        }

        return Available && _shader != null && _triangle != null && _output != null;
    }

    private bool Fit(uint width, uint height)
    {
        try
        {
            var spec = _output!.GetSpecification();
            if (spec.Width != width || spec.Height != height)
                _output.Resize(width, height);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Selection outline disabled: framebuffer resize failed");
            Available = false;
            return false;
        }
    }

    private void Draw(uint colorTextureId, uint entityTextureId, uint width, uint height, int entityId)
    {
        _output!.Bind();
        rendererApi.SetViewport(0, 0, width, height);
        rendererApi.SetDepthTest(false);
        rendererApi.SetBlend(false);
        rendererApi.SetFaceCulling(false);
        try
        {
            _shader!.Bind();
            rendererApi.BindTexture2D(colorTextureId, 0);
            rendererApi.BindTexture2D(entityTextureId, 1);
            _shader.SetInt("u_Id", entityId);
            rendererApi.DrawArrays(_triangle!, 3);
            _shader.Unbind();
        }
        finally
        {
            rendererApi.SetDepthTest(true);
            rendererApi.SetBlend(true);
            rendererApi.SetFaceCulling(true);
            _output.Unbind();
        }
    }

    private static FrameBufferSpecification ColorTarget(uint width, uint height) =>
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
