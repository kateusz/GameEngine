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
            default:
                throw new ArgumentOutOfRangeException();
        }

        throw new ArgumentOutOfRangeException();
    }
}