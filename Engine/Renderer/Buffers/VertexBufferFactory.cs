using Engine.Platform.OpenTK;
using Engine.Platform.SilkNet.Buffers;

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
                return new OpenTKVertexBuffer(vertices);
            case ApiType.SilkNet:
            default:
                throw new ArgumentOutOfRangeException();
        }
    
        throw new ArgumentOutOfRangeException();
    }
    
    public static IVertexBuffer Create(uint size)
    {
        switch (RendererApiType.Type)
        {
            case ApiType.None:
                break;
            case ApiType.SilkNet:
                return new SilkNetVertexBuffer(size);
            case ApiType.OpenTK:
            default:
                throw new ArgumentOutOfRangeException();
        }

        throw new ArgumentOutOfRangeException();
    }
}