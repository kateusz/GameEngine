using Engine.Platform.OpenGL;

namespace Engine.Renderer.Buffers;

public static class VertexBufferFactory
{
    public static IVertexBuffer Create(float[] vertices)
    {
        switch (RendererApiType.Type)
        {
            case ApiType.None:
                break;
            case ApiType.OpenGL:
                return new OpenGLVertexBuffer(vertices);
            default:
                throw new ArgumentOutOfRangeException();
        }

        throw new ArgumentOutOfRangeException();
    }
}