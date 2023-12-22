using Engine.Platform.OpenGL;
using Engine.Renderer;

namespace Engine.Core.Window;

public class WindowFactory
{
    public static IWindow Create(WindowProps props)
    {
        switch (RendererApiType.Type)
        {
            case ApiType.None:
                break;
            case ApiType.OpenGL:
                return new OpenGLWindow(props);
            default:
                throw new ArgumentOutOfRangeException();
        }

        throw new ArgumentOutOfRangeException();
    }
}