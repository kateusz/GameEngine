using Engine.Core;
using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Shaders;

namespace Engine.Renderer.PostProcessing;

/// <summary>
/// Extracts bright HDR regions and applies a two-pass Gaussian blur (LearnOpenGL bloom).
/// </summary>
public sealed class BloomPass(
    IRendererAPI rendererApi,
    IShaderFactory shaderFactory,
    IVertexArrayFactory vertexArrayFactory,
    IFrameBufferFactory frameBufferFactory) : IDisposable
{
    private const int BlurIterations = 10;

    private IShader? _extractShader;
    private IShader? _blurShader;
    private IVertexArray? _emptyVao;
    private IFrameBuffer? _extract;
    private IFrameBuffer? _pingPong0;
    private IFrameBuffer? _pingPong1;
    private bool _disposed;

    /// <summary>
    /// Blurs extracted highlights. Returned framebuffer is owned by this pass — do not dispose it.
    /// </summary>
    public IFrameBuffer Apply(uint hdrColorAttachmentId, uint width, uint height, float threshold)
    {
        EnsureSize(width, height);

        rendererApi.SetDepthTest(false);

        _extract!.Bind();
        rendererApi.SetClearColor(System.Numerics.Vector4.Zero);
        rendererApi.Clear();
        _extractShader!.Bind();
        _extractShader.SetFloat("u_Threshold", threshold);
        rendererApi.BindTexture2D(hdrColorAttachmentId, 0);
        rendererApi.DrawArrays(_emptyVao!, 3);
        _extract.Unbind();

        var readId = _extract.GetColorAttachmentRendererId();
        var horizontal = true;
        IFrameBuffer written = _pingPong0!;
        _blurShader!.Bind();
        for (var i = 0; i < BlurIterations; i++)
        {
            written = horizontal ? _pingPong1! : _pingPong0!;
            written.Bind();
            _blurShader.SetInt("u_Horizontal", horizontal ? 1 : 0);
            rendererApi.BindTexture2D(readId, 0);
            rendererApi.DrawArrays(_emptyVao!, 3);
            written.Unbind();
            readId = written.GetColorAttachmentRendererId();
            horizontal = !horizontal;
        }

        rendererApi.SetDepthTest(true);
        return written;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _emptyVao?.Dispose();
        _extract?.Dispose();
        _pingPong0?.Dispose();
        _pingPong1?.Dispose();
        _disposed = true;
    }

    private void EnsureInitialized()
    {
        if (_extractShader is not null)
            return;

        var vert = PathBuilder.Resolve("assets/shaders/OpenGL/hdrTonemap.vert");
        _extractShader = shaderFactory.Create(vert, PathBuilder.Resolve("assets/shaders/OpenGL/bloomExtract.frag"));
        _extractShader.Bind();
        _extractShader.SetInt("u_HdrBuffer", 0);
        _extractShader.Unbind();

        _blurShader = shaderFactory.Create(vert, PathBuilder.Resolve("assets/shaders/OpenGL/bloomBlur.frag"));
        _blurShader.Bind();
        _blurShader.SetInt("u_Image", 0);
        _blurShader.Unbind();

        _emptyVao = vertexArrayFactory.Create();
    }

    private void EnsureSize(uint width, uint height)
    {
        if (_extract is null)
        {
            _extract = frameBufferFactory.Create(BloomSpec(width, height));
            _pingPong0 = frameBufferFactory.Create(BloomSpec(width, height));
            _pingPong1 = frameBufferFactory.Create(BloomSpec(width, height));
            return;
        }

        var spec = _extract.GetSpecification();
        if (spec.Width == width && spec.Height == height)
            return;

        _extract.Resize(width, height);
        _pingPong0!.Resize(width, height);
        _pingPong1!.Resize(width, height);
    }

    private static FrameBufferSpecification BloomSpec(uint width, uint height) =>
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

    public void Initialize()
    {
        EnsureInitialized();
    }
}
