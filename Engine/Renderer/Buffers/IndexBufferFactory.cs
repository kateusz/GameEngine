using Engine.Platform.SilkNet.Buffers;

namespace Engine.Renderer.Buffers;

public static class IndexBufferFactory
{
    public static IIndexBuffer Create(uint[] indices, int count)
    {
        switch (RendererApiType.Type)
        {
            case ApiType.None:
                break;
            case ApiType.SilkNet:
                return new SilkNetIndexBuffer(indices, count);
            default:
                throw new ArgumentOutOfRangeException();
        }

        throw new ArgumentOutOfRangeException();
    }
}