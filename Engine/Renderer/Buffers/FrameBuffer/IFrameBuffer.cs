namespace Engine.Renderer.Buffers.FrameBuffer;

public interface IFrameBuffer : IBindable
{
    uint GetColorAttachmentRendererId();
    FrameBufferSpecification GetSpecification();
    void Resize(uint width, uint height);
    int ReadPixel(int attachmentIndex, int x, int y);
    void ClearAttachment(int attachmentIndex, int value);
}