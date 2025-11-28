using Engine.Core;
using Engine.Platform.SilkNet.Buffers;

namespace Engine.Renderer.Buffers.FrameBuffer;

internal sealed class FrameBufferFactory : IFrameBufferFactory
{
    private readonly IRendererApiConfig _apiConfig;

    /// <summary>
    /// Initializes a new instance of the FrameBufferFactory class.
    /// </summary>
    /// <param name="apiConfig">The renderer API configuration.</param>
    public FrameBufferFactory(IRendererApiConfig apiConfig)
    {
        _apiConfig = apiConfig ?? throw new ArgumentNullException(nameof(apiConfig));
    }
    
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
        
        return _apiConfig.Type switch
        {
            ApiType.SilkNet => new SilkNetFrameBuffer(frameBufferSpec),
            _ => throw new NotSupportedException($"Unsupported Render API type: {_apiConfig.Type}")
        };
    }
}
