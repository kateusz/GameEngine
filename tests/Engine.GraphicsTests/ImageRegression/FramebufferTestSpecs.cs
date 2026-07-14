using Engine.Renderer.Buffers.FrameBuffer;

namespace Engine.GraphicsTests.ImageRegression;

internal static class FramebufferTestSpecs
{
    public const int Width = 64;
    public const int Height = 64;

    public static FrameBufferSpecification ColorAndEntityId() =>
        new(Width, Height)
        {
            AttachmentsSpec = new FrameBufferAttachmentSpecification([
                new FrameBufferTextureSpecification(FrameBufferTextureFormat.RGBA8),
                new FrameBufferTextureSpecification(FrameBufferTextureFormat.RED_INTEGER),
                new FrameBufferTextureSpecification(FrameBufferTextureFormat.Depth),
            ])
        };
}
