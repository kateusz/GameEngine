using Engine.Core;
using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Shaders;

namespace Engine.Renderer;

/// <summary>
/// Tonemaps an HDR color attachment into an SDR framebuffer (ACES + gamma).
/// </summary>
public sealed class HdrTonemapPass(
    IRendererAPI rendererApi,
    IShaderFactory shaderFactory,
    IVertexArrayFactory vertexArrayFactory) : IDisposable
{
    private IShader? _shader;
    private IVertexArray? _emptyVao;
    private bool _disposed;

    public void Apply(uint hdrColorAttachmentId, IFrameBuffer sdrTarget, float exposure)
    {
        EnsureInitialized();

        sdrTarget.Bind();
        rendererApi.SetDepthTest(false);
        rendererApi.SetClearColor(System.Numerics.Vector4.Zero);
        rendererApi.Clear();

        _shader!.Bind();
        _shader.SetFloat("u_Exposure", exposure);
        rendererApi.BindTexture2D(hdrColorAttachmentId, 0);
        rendererApi.DrawArrays(_emptyVao!, 3);
        _shader.Unbind();

        sdrTarget.Unbind();
        rendererApi.SetDepthTest(true);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _shader?.Dispose();
        _emptyVao?.Dispose();
        _disposed = true;
    }

    private void EnsureInitialized()
    {
        if (_shader is not null)
            return;

        _shader = shaderFactory.Create(
            PathBuilder.Resolve("assets/shaders/OpenGL/hdrTonemap.vert"),
            PathBuilder.Resolve("assets/shaders/OpenGL/hdrTonemap.frag"));
        _shader.Bind();
        _shader.SetInt("u_HdrBuffer", 0);
        _shader.Unbind();
        
        _emptyVao = vertexArrayFactory.Create();
    }
}
