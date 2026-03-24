using Engine.Core;
using Engine.Platform.OpenGL.Buffers;
using Engine.Renderer.Profiling;

namespace Engine.Renderer.Buffers.FrameBuffer;

internal sealed class FrameBufferFactory(IRendererApiConfig apiConfig, IPerformanceProfiler profiler) : IFrameBufferFactory
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
            ApiType.SilkNet => new OpenGLFrameBuffer(frameBufferSpec, profiler),
            _ => throw new NotSupportedException($"Unsupported Render API type: {apiConfig.Type}")
        };
    }
}
