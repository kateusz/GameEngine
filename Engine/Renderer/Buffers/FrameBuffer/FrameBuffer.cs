namespace Engine.Renderer.Buffers.FrameBuffer;

internal abstract class FrameBuffer : IFrameBuffer
{
    public abstract void Bind();

    public abstract void Unbind();

    public abstract uint GetColorAttachmentRendererId();

    public abstract FrameBufferSpecification GetSpecification();

    public abstract void Resize(uint width, uint height);
    public abstract int ReadPixel(int attachmentIndex, int x, int y);
    public abstract void ClearAttachment(int attachmentIndex, int value);
    public abstract void Dispose();
}