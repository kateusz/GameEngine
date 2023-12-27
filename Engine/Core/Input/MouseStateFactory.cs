using Engine.Platform.OpenGL.Input;
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
            case ApiType.OpenGL:
                return new OpenGLMouseState();
            default:
                throw new ArgumentOutOfRangeException();
        }

        throw new ArgumentOutOfRangeException();
    }
}