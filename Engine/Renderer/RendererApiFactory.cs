using Engine.Platform.SilkNet;

namespace Engine.Renderer;

internal sealed class RendererApiFactory(IRendererApiConfig apiConfig) : IRendererApiFactory
{
    public IRendererAPI Create()
    {
        return apiConfig.Type switch
        {
            ApiType.SilkNet => new SilkNetRendererApi(),
            _ => throw new NotSupportedException($"Unsupported Render API type: {apiConfig.Type}")
        };
    }
}
