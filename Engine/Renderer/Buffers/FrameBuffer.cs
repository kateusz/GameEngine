namespace Engine.Renderer.Buffers;

public abstract class FrameBuffer : IFrameBuffer
{
    public abstract void Bind();

    public abstract void Unbind();

    public abstract uint GetColorAttachmentRendererId();

    public abstract FrameBufferSpecification GetSpecification();

    public abstract void Resize(uint width, uint height);
}