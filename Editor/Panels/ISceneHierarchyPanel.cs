using ECS;
using Editor.Core;
using Engine.Scene;

namespace Editor.Panels;

/// <summary>
/// Interface for the scene hierarchy panel that displays the entity tree.
/// </summary>
public interface ISceneHierarchyPanel : IEditorPanel
{
    /// <summary>
    /// Renders the scene hierarchy panel using ImGui.
    /// </summary>
    void Draw();

    /// <summary>
    /// Sets the scene context for the panel.
    /// </summary>
    /// <param name="scene">Scene to display</param>
    void SetContext(IScene? scene);

    /// <summary>
    /// Gets the currently selected entity.
    /// </summary>
    /// <returns>Selected entity, or null if none selected</returns>
    Entity? GetSelectedEntity();

    /// <summary>
    /// Sets the currently selected entity.
    /// </summary>
    /// <param name="entity">Entity to select</param>
    void SetSelectedEntity(Entity? entity);
    
    Action<Entity> EntitySelected { get; set; }
}
