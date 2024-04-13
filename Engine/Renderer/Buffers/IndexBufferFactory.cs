using Engine.Platform.OpenGL;
using Engine.Platform.SilkNet;

namespace Engine.Renderer.Buffers;

public static class IndexBufferFactory
{
    public static IIndexBuffer Create(uint[] indices, int count)
    {
        switch (RendererApiType.Type)
        {
            case ApiType.None:
                break;
            case ApiType.OpenTK:
                return new OpenGLIndexBuffer(indices, count);
            case ApiType.SilkNet:
                return new SilkNetIndexBuffer(indices, count);
            default:
                throw new ArgumentOutOfRangeException();
        }

        throw new ArgumentOutOfRangeException();
    }
}