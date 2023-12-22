using Engine.Platform.OpenGL;

namespace Engine.Renderer.Buffers;

public static class IndexBufferFactory
{
    public static IIndexBuffer Create(uint[] indices, int count)
    {
        switch (RendererApiType.Type)
        {
            case ApiType.None:
                break;
            case ApiType.OpenGL:
                return new OpenGLIndexBuffer(indices, count);
            default:
                throw new ArgumentOutOfRangeException();
        }

        throw new ArgumentOutOfRangeException();
    }
}