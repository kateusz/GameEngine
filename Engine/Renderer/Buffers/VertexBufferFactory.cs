using Engine.Platform.SilkNet.Buffers;

namespace Engine.Renderer.Buffers;

public static class VertexBufferFactory
{
    public static IVertexBuffer Create(uint size)
    {
        return RendererApiType.Type switch
        {
            ApiType.SilkNet => new SilkNetVertexBuffer(size),
            _ => throw new NotSupportedException($"Unsupported Render API type: {RendererApiType.Type}")
        };
    }
}