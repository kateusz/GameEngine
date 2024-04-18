using Engine.Renderer.Buffers;

namespace Engine.Renderer;

public class FrameBuffer : IFrameBuffer
{
    public virtual void Bind()
    {
    }

    public virtual void Unbind()
    {
    }
    
    public virtual uint GetColorAttachmentRendererId()
    {
        return 0;
    }

    public virtual FramebufferSpecification GetSpecification()
    {
        // TODO:
        return null;
    }
}