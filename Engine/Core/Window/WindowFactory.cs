using Engine.Platform.OpenTK;
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
            case ApiType.OpenTK:
                return new OpenTKGameWindow(props);
            case ApiType.SilkNet:
                return new SilkNetGameWindow(props);
            default:
                throw new ArgumentOutOfRangeException();
        }

        throw new ArgumentOutOfRangeException();
    }
}