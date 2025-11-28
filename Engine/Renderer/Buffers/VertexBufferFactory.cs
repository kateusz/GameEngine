using Engine.Platform.SilkNet.Buffers;

namespace Engine.Renderer.Buffers;

public class VertexBufferFactory : IVertexBufferFactory
{
    private readonly IRendererApiConfig _apiConfig;

    /// <summary>
    /// Initializes a new instance of the VertexBufferFactory class.
    /// </summary>
    /// <param name="apiConfig">The renderer API configuration.</param>
    public VertexBufferFactory(IRendererApiConfig apiConfig)
    {
        _apiConfig = apiConfig ?? throw new ArgumentNullException(nameof(apiConfig));
    }
    
    public IVertexBuffer Create(uint size)
    {
        return _apiConfig.Type switch
        {
            ApiType.SilkNet => new SilkNetVertexBuffer(size),
            _ => throw new NotSupportedException($"Unsupported Render API type: {_apiConfig.Type}")
        };
    }
}
