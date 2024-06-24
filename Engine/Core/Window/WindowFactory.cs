using Engine.Platform.SilkNet;
using Engine.Renderer;

namespace Engine.Core.Window;

public class WindowFactory
{
    public static IGameWindow Create(WindowProps props)
    {
        switch (RendererApiType.Type)
        {
            case ApiType.None:
                break;
            case ApiType.SilkNet:
                return new SilkNetGameWindow(props);
            default:
                throw new ArgumentOutOfRangeException();
        }

        throw new ArgumentOutOfRangeException();
    }
}