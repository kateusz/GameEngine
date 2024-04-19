namespace Engine.Renderer.Buffers;

public interface IFrameBuffer : IBindable
{
    uint GetColorAttachmentRendererId();
    FrameBufferSpecification GetSpecification();
}