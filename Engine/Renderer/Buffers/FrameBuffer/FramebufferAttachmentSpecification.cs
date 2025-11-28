namespace Engine.Renderer.Buffers.FrameBuffer;

public sealed class FramebufferAttachmentSpecification
{
    public readonly List<FramebufferTextureSpecification> Attachments;

    public FramebufferAttachmentSpecification(List<FramebufferTextureSpecification> attachments)
    {
        Attachments = attachments;
    }
}