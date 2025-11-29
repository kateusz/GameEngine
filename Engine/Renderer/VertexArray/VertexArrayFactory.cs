using Engine.Platform.SilkNet;

namespace Engine.Renderer.VertexArray;

internal sealed class VertexArrayFactory(IRendererApiConfig apiConfig) : IVertexArrayFactory
{
    public IVertexArray Create()
    {
        return apiConfig.Type switch
        {
            ApiType.SilkNet => new SilkNetVertexArray(),
            _ => throw new NotSupportedException($"Unsupported Render API type: {apiConfig.Type}")
        };
    }
}
