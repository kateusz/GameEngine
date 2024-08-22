namespace Engine.Renderer.Buffers.FrameBuffer;

public class FramebufferAttachmentSpecification
{
    public List<FramebufferTextureSpecification> Attachments;

    public FramebufferAttachmentSpecification(List<FramebufferTextureSpecification> attachments)
    {
        Attachments = attachments;
    }
}