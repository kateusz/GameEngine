using Engine.Platform.SilkNet;
using Engine.Platform.SilkNet.Buffers;

namespace Engine.Renderer.Buffers;

public static class FrameBufferFactory
{
    public static IFrameBuffer Create(FrameBufferSpecification spec)
    {
        switch (RendererApiType.Type)
        {
            case ApiType.None:
                break;
            case ApiType.SilkNet:
                return new SilkNetFrameBuffer(spec);
            default:
                throw new ArgumentOutOfRangeException();
        }

        throw new ArgumentOutOfRangeException();
    }
}