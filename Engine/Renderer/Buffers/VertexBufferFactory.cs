using Engine.Platform.OpenGL;
using Engine.Platform.SilkNet;

namespace Engine.Renderer.Buffers;

public static class VertexBufferFactory
{
    public static IVertexBuffer Create(float[] vertices)
    {
        switch (RendererApiType.Type)
        {
            case ApiType.None:
                break;
            case ApiType.OpenTK:
                return new OpenGLVertexBuffer(vertices);
            case ApiType.SilkNet:
                return new SilkNetVertexBuffer(vertices);
            default:
                throw new ArgumentOutOfRangeException();
        }

        throw new ArgumentOutOfRangeException();
    }
}