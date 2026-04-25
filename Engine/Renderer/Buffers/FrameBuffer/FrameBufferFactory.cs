using Engine.Core;
using Engine.Platform.OpenGL.Buffers;

namespace Engine.Renderer.Buffers.FrameBuffer;

internal sealed class FrameBufferFactory(IRendererApiConfig apiConfig) : IFrameBufferFactory
{
    public IFrameBuffer Create()
    {
        var frameBufferSpec = new FrameBufferSpecification(
            DisplayConfig.DefaultEditorViewportWidth,
            DisplayConfig.DefaultEditorViewportHeight)
        {
            AttachmentsSpec = new FramebufferAttachmentSpecification([
                new FramebufferTextureSpecification(FramebufferTextureFormat.RGBA16F),
                new FramebufferTextureSpecification(FramebufferTextureFormat.RED_INTEGER),
                new FramebufferTextureSpecification(FramebufferTextureFormat.Depth),
            ])
        };

        return Create(frameBufferSpec);
    }

    public IFrameBuffer Create(FrameBufferSpecification specification)
    {
        return apiConfig.Type switch
        {
            ApiType.SilkNet => new OpenGLFrameBuffer(specification),
            _ => throw new NotSupportedException($"Unsupported Render API type: {apiConfig.Type}")
        };
    }
}
