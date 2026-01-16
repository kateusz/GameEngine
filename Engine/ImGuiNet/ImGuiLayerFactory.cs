using Engine.Renderer;

namespace Engine.ImGuiNet;

internal sealed class ImGuiLayerFactory(IRendererApiConfig apiConfig) : IImGuiLayerFactory
{
    public IImGuiLayer Create()
    {
        return apiConfig.Type switch
        {
            ApiType.OpenGL => new Platform.SilkNet.SilkNetImGuiLayer(),
            _ => throw new NotSupportedException($"Unsupported Render API type: {apiConfig.Type}")
        };
    }
}