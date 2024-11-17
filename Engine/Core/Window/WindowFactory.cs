using Engine.Platform.SilkNet;
using Engine.Renderer;

namespace Engine.Core.Window;

public class WindowFactory
{
    public static IGameWindow Create(WindowProps props)
    {
        return RendererApiType.Type switch
        {
            ApiType.SilkNet => new SilkNetGameWindow(props),
            _ => throw new NotSupportedException($"Unsupported Render API type: {RendererApiType.Type}")
        };
    }
}