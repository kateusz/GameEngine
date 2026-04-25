using Engine.Renderer.Buffers.FrameBuffer;

namespace Engine.Renderer;

public interface IHdrToneMapper : IDisposable
{
    void RenderToFramebuffer(uint sourceTextureId, IFrameBuffer targetFramebuffer, float exposure = 1.0f);
    void Init();
}
