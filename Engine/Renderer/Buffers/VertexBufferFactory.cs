using Engine.Platform.SilkNet.Buffers;

namespace Engine.Renderer.Buffers;

internal sealed class VertexBufferFactory(IRendererApiConfig apiConfig) : IVertexBufferFactory
{
    public IVertexBuffer Create(uint size)
    {
        return apiConfig.Type switch
        {
            ApiType.SilkNet => new SilkNetVertexBuffer(size),
            _ => throw new NotSupportedException($"Unsupported Render API type: {apiConfig.Type}")
        };
    }
}
