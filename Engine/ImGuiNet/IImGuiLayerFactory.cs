namespace Engine.ImGuiNet;

/// <summary>
/// Factory for creating platform-specific ImGui layer implementations.
/// </summary>
public interface IImGuiLayerFactory
{
    IImGuiLayer Create();
}
