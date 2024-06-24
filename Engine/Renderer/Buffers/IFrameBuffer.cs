namespace Engine.Renderer.Buffers;

public interface IFrameBuffer : IBindable
{
    uint GetColorAttachmentRendererId();
    FrameBufferSpecification GetSpecification();
    void Resize(uint width, uint height);
}