using Engine.Core.Input;
using Engine.Platform.SilkNet;
using Engine.Renderer;
using Silk.NET.Windowing;

namespace Engine.Core.Window;

public static class GameWindowFactory
{
    public static IGameWindow Create(IWindow window)
    {
        return RendererApiType.Type switch
        {
            ApiType.SilkNet => new SilkNetGameWindow(window),
            _ => throw new NotSupportedException($"Unsupported Render API type: {RendererApiType.Type}")
        };
    }
}