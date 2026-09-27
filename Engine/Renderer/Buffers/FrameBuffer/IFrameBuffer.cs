namespace Engine.Renderer.Buffers.FrameBuffer;

public interface IFrameBuffer : IBindable
{
    uint GetColorAttachmentRendererId(int index = 0);
    uint GetDepthAttachmentRendererId();
    FrameBufferSpecification GetSpecification();
    void Resize(uint width, uint height);
    int ReadPixel(int attachmentIndex, int x, int y);
    void ClearAttachment(int attachmentIndex, int value);
    void BindDepthCubemapFace(int face);
}