using Engine.Core.Input;
using Engine.Platform.SilkNet;
using Engine.Renderer;
using Silk.NET.Windowing;

namespace Engine.Core.Window;

internal sealed class GameWindowFactory(IRendererApiConfig apiConfig, IWindow window, IInputSystemFactory inputSystemFactory) : IGameWindowFactory
{
    public IGameWindow Create()
    {
        return apiConfig.Type switch
        {
            ApiType.OpenGL => new SilkNetGameWindow(window, inputSystemFactory),
            _ => throw new NotSupportedException($"Unsupported Render API type: {apiConfig.Type}")
        };
    }
}
