using Engine.Platform.OpenGL;
using Engine.Platform.SilkNet;

namespace Engine.Renderer;

public static class RendererApiFactory
{
    public static IRendererAPI Create()
    {
        switch (RendererApiType.Type)
        {
            case ApiType.None:
                break;
            case ApiType.OpenTK:
                return new OpenGLRendererAPI();
            case ApiType.SilkNet:
                return new SilkNetRendererAPI();
            default:
                throw new ArgumentOutOfRangeException();
        }

        throw new ArgumentOutOfRangeException();
    }
}