using Engine.Platform.OpenGL.Input;
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
            case ApiType.OpenGL:
                return new OpenGLKeyboardState();
            default:
                throw new ArgumentOutOfRangeException();
        }

        throw new ArgumentOutOfRangeException();
    }
}