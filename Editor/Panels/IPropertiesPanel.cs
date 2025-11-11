using ECS;
using Editor.Core;

namespace Editor.Panels;

/// <summary>
/// Interface for the properties/inspector panel that displays component properties.
/// </summary>
public interface IPropertiesPanel : IEditorPanel
{
    /// <summary>
    /// Renders the properties panel using ImGui.
    /// </summary>
    void Draw();

    void SetSelectedEntity(Entity? entity);
}
