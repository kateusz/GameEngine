using Engine.Platform.SilkNet;

namespace Engine.Renderer.VertexArray;

public static class VertexArrayFactory
{
    public static IVertexArray Create()
    {
        return RendererApiType.Type switch
        {
            ApiType.SilkNet => new SilkNetVertexArray(),
            _ => throw new NotSupportedException($"Unsupported Render API type: {RendererApiType.Type}")
        };
    }
}