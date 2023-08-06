using Engine.Platform.OpenGL;

namespace Engine.Renderer;

public static class VertexBufferFactory
{
    public static IVertexBuffer Create(float[] vertices)
    {
        switch (Renderer.RendererApi)
        {
            case RendererApi.None:
                break;
            case RendererApi.OpenGL:
                return new OpenGLVertexBuffer(vertices);
            default:
                throw new ArgumentOutOfRangeException();
        }

        throw new ArgumentOutOfRangeException();
    }
}