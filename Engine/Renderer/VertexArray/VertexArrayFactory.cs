using Engine.Platform.SilkNet;

namespace Engine.Renderer.VertexArray;

internal sealed class VertexArrayFactory : IVertexArrayFactory
{
    private readonly IRendererApiConfig _apiConfig;

    /// <summary>
    /// Initializes a new instance of the VertexArrayFactory class.
    /// </summary>
    /// <param name="apiConfig">The renderer API configuration.</param>
    public VertexArrayFactory(IRendererApiConfig apiConfig)
    {
        _apiConfig = apiConfig ?? throw new ArgumentNullException(nameof(apiConfig));
    }

    public IVertexArray Create()
    {
        return _apiConfig.Type switch
        {
            ApiType.SilkNet => new SilkNetVertexArray(),
            _ => throw new NotSupportedException($"Unsupported Render API type: {_apiConfig.Type}")
        };
    }
}
