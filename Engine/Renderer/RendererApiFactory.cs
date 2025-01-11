using Engine.Platform.SilkNet;

namespace Engine.Renderer;

public static class RendererApiFactory
{
    public static IRendererAPI Create()
    {
        return RendererApiType.Type switch
        {
            ApiType.SilkNet => new SilkNetRendererApi(),
            _ => throw new NotSupportedException($"Unsupported Render API type: {RendererApiType.Type}")
        };
    }
}