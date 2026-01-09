using Engine.Core;
using Engine.Platform.OpenGL.Buffers;

namespace Engine.Renderer.Buffers.FrameBuffer;

internal sealed class FrameBufferFactory(IRendererApiConfig apiConfig) : IFrameBufferFactory
{
    public IFrameBuffer Create()
    {
        // TODO: move to DI
        var frameBufferSpec = new FrameBufferSpecification(DisplayConfig.DefaultEditorViewportWidth,
            DisplayConfig.DefaultEditorViewportHeight)
        {
            AttachmentsSpec = new FramebufferAttachmentSpecification([
                new FramebufferTextureSpecification(FramebufferTextureFormat.RGBA8),
                new FramebufferTextureSpecification(FramebufferTextureFormat.RED_INTEGER),
                new FramebufferTextureSpecification(FramebufferTextureFormat.Depth),
            ])
        };
        
        return apiConfig.Type switch
        {
            ApiType.OpenGL => new OpenGLFrameBuffer(frameBufferSpec),
            _ => throw new NotSupportedException($"Unsupported Render API type: {apiConfig.Type}")
        };
    }
}
