using Engine.Platform.SilkNet;
using Engine.Renderer;
using Silk.NET.Windowing;

namespace Engine.Core.Window;

internal sealed class GameWindowFactory : IGameWindowFactory
{
    private readonly IRendererApiConfig _apiConfig;
    private readonly IWindow _window;

    /// <summary>
    /// Initializes a new instance of the GameWindowFactory class.
    /// </summary>
    /// <param name="apiConfig">The renderer API configuration.</param>
    /// <param name="window"></param>
    public GameWindowFactory(IRendererApiConfig apiConfig, IWindow window)
    {
        _apiConfig = apiConfig ?? throw new ArgumentNullException(nameof(apiConfig));
        _window = window ?? throw new ArgumentNullException(nameof(window));
    }

    public IGameWindow Create()
    {
        return _apiConfig.Type switch
        {
            ApiType.SilkNet => new SilkNetGameWindow(_window),
            _ => throw new NotSupportedException($"Unsupported Render API type: {_apiConfig.Type}")
        };
    }
}
