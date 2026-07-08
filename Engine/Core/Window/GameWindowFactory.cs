using Engine.Core.Input;
using Engine.Platform.SilkNet;
using Engine.Renderer;
using Silk.NET.Windowing;

namespace Engine.Core.Window;

internal sealed class GameWindowFactory(
    IRendererApiConfig apiConfig,
    IWindow window,
    IInputSystemFactory inputSystemFactory,
    IGraphicsContext graphicsContext) : IGameWindowFactory
{
    public IGameWindow Create()
    {
        return apiConfig.Type switch
        {
            ApiType.SilkNet => new SilkNetGameWindow(window, inputSystemFactory, graphicsContext),
            _ => throw new NotSupportedException($"Unsupported Render API type: {apiConfig.Type}")
        };
    }
}
