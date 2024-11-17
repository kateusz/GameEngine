using Engine.Platform.SilkNet.Buffers;

namespace Engine.Renderer.Buffers;

public static class IndexBufferFactory
{
    public static IIndexBuffer Create(uint[] indices, int count)
    {
        return RendererApiType.Type switch
        {
            ApiType.SilkNet => new SilkNetIndexBuffer(indices, count),
            _ => throw new NotSupportedException($"Unsupported Render API type: {RendererApiType.Type}")
        };
    }
}