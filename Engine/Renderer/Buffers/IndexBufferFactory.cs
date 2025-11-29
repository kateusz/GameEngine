using Engine.Platform.SilkNet.Buffers;

namespace Engine.Renderer.Buffers;

internal sealed class IndexBufferFactory(IRendererApiConfig apiConfig) : IIndexBufferFactory
{
    public IIndexBuffer Create(uint[] indices, int count)
    {
        return apiConfig.Type switch
        {
            ApiType.SilkNet => new SilkNetIndexBuffer(indices, count),
            _ => throw new NotSupportedException($"Unsupported Render API type: {apiConfig.Type}")
        };
    }
}
