using Engine.Core;
using Engine.Platform.OpenGL.Buffers;
using Engine.Renderer.Buffers.FrameBuffer;

namespace Engine.Platform.OpenGL;

internal sealed class FrameBufferFactory : IFrameBufferFactory
{
    public IFrameBuffer Create()
    {
        var frameBufferSpec = new FrameBufferSpecification(
            DisplayConfig.DefaultEditorViewportWidth,
            DisplayConfig.DefaultEditorViewportHeight)
        {
            AttachmentsSpec = new FrameBufferAttachmentSpecification([
                new FrameBufferTextureSpecification(FrameBufferTextureFormat.RGBA8),
                new FrameBufferTextureSpecification(FrameBufferTextureFormat.RED_INTEGER),
                new FrameBufferTextureSpecification(FrameBufferTextureFormat.Depth),
            ])
        };

        return Create(frameBufferSpec);
    }

    public IFrameBuffer Create(FrameBufferSpecification specification)
    {
        return new OpenGLFrameBuffer(specification);
    }
}
