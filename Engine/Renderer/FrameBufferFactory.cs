using Engine.Platform.SilkNet;
using Engine.Renderer.Buffers;

namespace Engine.Renderer;

public static class FrameBufferFactory
{
    public static IFrameBuffer Create(FramebufferSpecification spec)
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