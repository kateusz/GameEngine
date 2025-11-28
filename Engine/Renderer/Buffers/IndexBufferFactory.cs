using Engine.Platform.SilkNet.Buffers;

namespace Engine.Renderer.Buffers;

public class IndexBufferFactory : IIndexBufferFactory
{
    private readonly IRendererApiConfig _apiConfig;

    /// <summary>
    /// Initializes a new instance of the IndexBufferFactory class.
    /// </summary>
    /// <param name="apiConfig">The renderer API configuration.</param>
    public IndexBufferFactory(IRendererApiConfig apiConfig)
    {
        _apiConfig = apiConfig ?? throw new ArgumentNullException(nameof(apiConfig));
    }
    
    public IIndexBuffer Create(uint[] indices, int count)
    {
        return _apiConfig.Type switch
        {
            ApiType.SilkNet => new SilkNetIndexBuffer(indices, count),
            _ => throw new NotSupportedException($"Unsupported Render API type: {_apiConfig.Type}")
        };
    }
}
