using Engine.Platform.SilkNet;
using Engine.Renderer;
using Silk.NET.Windowing;

namespace Engine.Core.Window;

internal sealed class GameWindowFactory(IRendererApiConfig apiConfig, IWindow window) : IGameWindowFactory
{
    public IGameWindow Create()
    {
        return apiConfig.Type switch
        {
            ApiType.SilkNet => new SilkNetGameWindow(window),
            _ => throw new NotSupportedException($"Unsupported Render API type: {apiConfig.Type}")
        };
    }
}
