using Engine.Platform.SilkNet.Buffers;

namespace Engine.Renderer.Buffers.FrameBuffer;

public static class FrameBufferFactory
{
    public static IFrameBuffer Create(FrameBufferSpecification spec)
    {
        return RendererApiType.Type switch
        {
            ApiType.SilkNet => new SilkNetFrameBuffer(spec),
            _ => throw new NotSupportedException($"Unsupported Render API type: {RendererApiType.Type}")
        };
    }
}