namespace Engine.Renderer.Buffers.FrameBuffer;

internal abstract class FrameBuffer : IFrameBuffer
{

    public abstract void Bind();

    public abstract void Unbind();

    public abstract uint GetColorAttachmentRendererId();

    public abstract uint GetDepthAttachmentRendererId();

    public abstract FrameBufferSpecification GetSpecification();

    public abstract void Resize(uint width, uint height);
    public abstract int ReadPixel(int attachmentIndex, int x, int y);
    public abstract void ClearAttachment(int attachmentIndex, int value);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected abstract void Dispose(bool disposing);
}
