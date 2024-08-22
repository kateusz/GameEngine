namespace Engine.Renderer.Buffers.FrameBuffer;

public abstract class FrameBuffer : IFrameBuffer
{
    public abstract void Bind();

    public abstract void Unbind();

    public abstract uint GetColorAttachmentRendererId(uint index = 0);

    public abstract FrameBufferSpecification GetSpecification();

    public abstract void Resize(uint width, uint height);
}