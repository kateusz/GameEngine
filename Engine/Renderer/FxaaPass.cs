using System.Numerics;
using Engine.Core;
using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Shaders;

namespace Engine.Renderer;

/// <summary>
/// Fast approximate AA on an SDR color attachment (run after tonemap).
/// </summary>
public sealed class FxaaPass(
    IRendererAPI rendererApi,
    IShaderFactory shaderFactory,
    IVertexArrayFactory vertexArrayFactory,
    IFrameBufferFactory frameBufferFactory) : IDisposable
{
    private IShader? _shader;
    private IVertexArray? _emptyVao;
    private IFrameBuffer? _output;
    private bool _disposed;

    /// <summary>
    /// Anti-aliases <paramref name="sdrColorAttachmentId"/>. Returned framebuffer is owned by this pass.
    /// </summary>
    public IFrameBuffer Apply(uint sdrColorAttachmentId, uint width, uint height)
    {
        EnsureSize(width, height);
        ApplyTo(sdrColorAttachmentId, _output, width, height);
        return _output!;
    }

    /// <param name="sdrTarget">
    /// Destination. Null draws into the currently bound framebuffer (backbuffer after an SDR unbind).
    /// </param>
    public void ApplyTo(uint sdrColorAttachmentId, IFrameBuffer? sdrTarget, uint width, uint height)
    {
        EnsureInitialized();

        var invW = 1f / MathF.Max(width, 1);
        var invH = 1f / MathF.Max(height, 1);

        sdrTarget?.Bind();
        rendererApi.SetDepthTest(false);
        rendererApi.SetClearColor(Vector4.Zero);
        rendererApi.Clear();

        _shader!.Bind();
        _shader.SetFloat("u_InverseWidth", invW);
        _shader.SetFloat("u_InverseHeight", invH);
        rendererApi.BindTexture2D(sdrColorAttachmentId, 0);
        rendererApi.DrawArrays(_emptyVao!, 3);
        _shader.Unbind();

        sdrTarget?.Unbind();
        rendererApi.SetDepthTest(true);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _shader?.Dispose();
        _emptyVao?.Dispose();
        _output?.Dispose();
        _disposed = true;
    }

    public void Initialize() => EnsureInitialized();

    private void EnsureInitialized()
    {
        if (_shader is not null)
            return;

        _shader = shaderFactory.Create(
            PathBuilder.Resolve("assets/shaders/OpenGL/hdrTonemap.vert"),
            PathBuilder.Resolve("assets/shaders/OpenGL/fxaa.frag"));
        _shader.Bind();
        _shader.SetInt("u_Texture", 0);
        _shader.Unbind();
        _emptyVao = vertexArrayFactory.Create();
    }

    private void EnsureSize(uint width, uint height)
    {
        EnsureInitialized();
        if (_output is null)
        {
            _output = frameBufferFactory.Create(SdrSpec(width, height));
            return;
        }

        var spec = _output.GetSpecification();
        if (spec.Width == width && spec.Height == height)
            return;

        _output.Resize(width, height);
    }

    private static FrameBufferSpecification SdrSpec(uint width, uint height) =>
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
