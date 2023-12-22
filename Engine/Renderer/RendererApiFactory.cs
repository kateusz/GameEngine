using Engine.Platform.OpenGL;

namespace Engine.Renderer;

public static class RendererApiFactory
{
    public static IRendererAPI Create()
    {
        switch (RendererApiType.Type)
        {
            case ApiType.None:
                break;
            case ApiType.OpenGL:
                return new OpenGLRendererAPI();
            default:
                throw new ArgumentOutOfRangeException();
        }

        throw new ArgumentOutOfRangeException();
    }
}