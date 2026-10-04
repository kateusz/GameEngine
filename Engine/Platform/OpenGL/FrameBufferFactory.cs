using Engine.Core;
using Engine.Core.Window;
using Engine.Platform.OpenGL.Buffers;
using Engine.Renderer.Buffers.FrameBuffer;

namespace Engine.Platform.OpenGL;

internal sealed class FrameBufferFactory : IFrameBufferFactory
{
    public IFrameBuffer Create()
    {
        return Create(SceneTarget(
            DisplayConfig.DefaultEditorViewportWidth,
            DisplayConfig.DefaultEditorViewportHeight));
    }

    internal static FrameBufferSpecification SceneTarget(uint width, uint height) =>
        new(width, height)
        {
            AttachmentsSpec = new FrameBufferAttachmentSpecification([
                new FrameBufferTextureSpecification(FrameBufferTextureFormat.RGBA16F)
                {
                    Filter = FrameBufferTextureFilter.Linear,
                    Wrap = FrameBufferTextureWrap.ClampToEdge
                },
                new FrameBufferTextureSpecification(FrameBufferTextureFormat.RED_INTEGER),
                new FrameBufferTextureSpecification(FrameBufferTextureFormat.Depth),
            ])
        };

    public IFrameBuffer Create(FrameBufferSpecification specification)
    {
        return new OpenGLFrameBuffer(specification);
    }
}
