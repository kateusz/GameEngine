namespace Engine.Renderer.Buffers.FrameBuffer;

public sealed record FrameBufferAttachmentSpecification(IReadOnlyList<FrameBufferTextureSpecification> Attachments);
