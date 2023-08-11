using Engine.Platform.OpenGL;

namespace Engine.Renderer;

public static class VertexArrayFactory
{
    public static IVertexArray Create()
    {
        switch (RendererApiType.Type)
        {
            case ApiType.None:
                break;
            case ApiType.OpenGL:
                return new OpenGLVertexArray();
            default:
                throw new ArgumentOutOfRangeException();
        }

        throw new ArgumentOutOfRangeException();
    }
}