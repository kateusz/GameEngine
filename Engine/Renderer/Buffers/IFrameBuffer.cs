namespace Engine.Renderer.Buffers;

public interface IFrameBuffer : IBindable
{
    uint GetColorAttachmentRendererId();
    FramebufferSpecification GetSpecification();
}