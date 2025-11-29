using Engine.Platform.SilkNet;

namespace Engine.Renderer;

internal sealed class RendererApiFactory : IRendererApiFactory
{
    private readonly IRendererApiConfig _apiConfig;

    /// <summary>
    /// Initializes a new instance of the RendererApiFactory class.
    /// </summary>
    /// <param name="apiConfig">The renderer API configuration.</param>
    public RendererApiFactory(IRendererApiConfig apiConfig)
    {
        _apiConfig = apiConfig ?? throw new ArgumentNullException(nameof(apiConfig));
    }
    
    public IRendererAPI Create()
    {
        return _apiConfig.Type switch
        {
            ApiType.SilkNet => new SilkNetRendererApi(),
            _ => throw new NotSupportedException($"Unsupported Render API type: {_apiConfig.Type}")
        };
    }
}
