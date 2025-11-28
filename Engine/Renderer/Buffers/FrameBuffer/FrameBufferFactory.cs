using Engine.Platform.SilkNet.Buffers;

namespace Engine.Renderer.Buffers.FrameBuffer;

public class FrameBufferFactory : IFrameBufferFactory
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
    
    public IFrameBuffer Create(FrameBufferSpecification spec)
    {
        return _apiConfig.Type switch
        {
            ApiType.SilkNet => new SilkNetFrameBuffer(spec),
            _ => throw new NotSupportedException($"Unsupported Render API type: {_apiConfig.Type}")
        };
    }
}
