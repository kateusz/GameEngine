namespace Engine.Renderer.Buffers.FrameBuffer;

public interface IFrameBuffer : IBindable
{
    uint GetColorAttachmentRendererId(uint index = 0);
    FrameBufferSpecification GetSpecification();
    void Resize(uint width, uint height);
}