using Engine.Renderer;
using Ui.ImGui.Platform.SilkNet;

namespace Ui.ImGui;

public interface IImGuiLayerFactory
{
    IImGuiLayer Create();
}

internal sealed class ImGuiLayerFactory(IRendererApiConfig apiConfig) : IImGuiLayerFactory
{
    public IImGuiLayer Create()
    {
        return apiConfig.Type switch
        {
            ApiType.SilkNet => new SilkNetImGuiLayer(),
            _ => throw new NotSupportedException($"Unsupported Render API type: {apiConfig.Type}")
        };
    }
}
