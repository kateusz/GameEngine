using Engine.Platform.OpenTK.Input;
using Engine.Platform.SilkNet.Input;
using Engine.Renderer;

namespace Engine.Core.Input;

public class MouseStateFactory
{
    public static IMouseState Create()
    {
        switch (RendererApiType.Type)
        {
            case ApiType.None:
                break;
            case ApiType.OpenTK:
                return new OpenTKMouseState();
            case ApiType.SilkNet:
                return new SIlkNetMouseState();
            default:
                throw new ArgumentOutOfRangeException();
        }

        throw new ArgumentOutOfRangeException();
    }
}