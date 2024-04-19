using Engine.Renderer.Buffers;

namespace Engine.Renderer;

public abstract class FrameBuffer : IFrameBuffer
{
    public abstract void Bind();

    public abstract void Unbind();

    public abstract uint GetColorAttachmentRendererId();

    public abstract FramebufferSpecification GetSpecification();
}