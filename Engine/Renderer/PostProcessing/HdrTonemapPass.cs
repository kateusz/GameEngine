using Engine.Core;
using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Shaders;

namespace Engine.Renderer.PostProcessing;

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

    /// <param name="sdrTarget">
    /// SDR destination. Null draws into the currently bound framebuffer (backbuffer after an HDR unbind).
    /// </param>
    public void Apply(
        uint hdrColorAttachmentId,
        IFrameBuffer? sdrTarget,
        float exposure,
        uint bloomColorAttachmentId = 0,
        float bloomIntensity = 0f)
    {
        EnsureInitialized();

        sdrTarget?.Bind();
        rendererApi.SetDepthTest(false);
        rendererApi.SetClearColor(System.Numerics.Vector4.Zero);
        rendererApi.Clear();

        _shader!.Bind();
        _shader.SetFloat("u_Exposure", exposure);
        _shader.SetFloat("u_BloomIntensity", bloomColorAttachmentId == 0 ? 0f : bloomIntensity);
        rendererApi.BindTexture2D(hdrColorAttachmentId, 0);
        rendererApi.BindTexture2D(
            bloomColorAttachmentId != 0 ? bloomColorAttachmentId : hdrColorAttachmentId, 1);
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
        _shader.SetInt("u_BloomBlur", 1);
        _shader.Unbind();
        
        _emptyVao = vertexArrayFactory.Create();
    }
}
