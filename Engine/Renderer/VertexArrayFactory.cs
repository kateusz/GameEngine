using Engine.Platform.OpenGL;

namespace Engine.Renderer;

public static class VertexArrayFactory
{
    public static IVertexArray Create()
    {
        switch (Renderer.RendererApi)
        {
            case RendererApi.None:
                break;
            case RendererApi.OpenGL:
                return new OpenGLVertexArray();
            default:
                throw new ArgumentOutOfRangeException();
        }

        throw new ArgumentOutOfRangeException();
    }
}