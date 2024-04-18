using Engine.Platform.OpenTK.Input;
using Engine.Platform.SilkNet.Input;
using Engine.Renderer;

namespace Engine.Core.Input;

public class KeyboardStateFactory
{
    public static IKeyboardState Create()
    {
        switch (RendererApiType.Type)
        {
            case ApiType.None:
                break;
            case ApiType.OpenTK:
                return new OpenTKKeyboardState();
            case ApiType.SilkNet:
                return new SilkNetKeyboardState();
            default:
                throw new ArgumentOutOfRangeException();
        }

        throw new ArgumentOutOfRangeException();
    }
}