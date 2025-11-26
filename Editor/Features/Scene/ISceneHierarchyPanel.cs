using ECS;
using Engine.Scene;

namespace Editor.Features.Scene;

/// <summary>
/// Interface for the scene hierarchy panel that displays the entity tree.
/// </summary>
public interface ISceneHierarchyPanel
{
    /// <summary>
    /// Renders the scene hierarchy panel using ImGui.
    /// </summary>
    void Draw();

    /// <summary>
    /// Sets the scene context for the panel.
    /// </summary>
    /// <param name="scene">Scene to display</param>
    void SetScene(IScene scene);

    /// <summary>
    /// Gets the currently selected entity.
    /// </summary>
    /// <returns>Selected entity, or null if none selected</returns>
    Entity? GetSelectedEntity();

    /// <summary>
    /// Sets the currently selected entity.
    /// </summary>
    /// <param name="entity">Entity to select</param>
    void SetSelectedEntity(Entity entity);
    
    Action<Entity> EntitySelected { get; set; }
}
