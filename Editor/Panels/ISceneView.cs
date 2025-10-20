using ECS;
using Engine.Scene;

namespace Editor.Panels;

/// <summary>
/// Abstraction for scene view components that display and interact with scenes.
/// Decouples SceneManager from specific UI implementations.
/// </summary>
public interface ISceneView
{
    /// <summary>
    /// Sets the current scene context for the view.
    /// </summary>
    /// <param name="context">The scene to display, or null to clear the context.</param>
    void SetContext(Scene? context);
    
    /// <summary>
    /// Gets the currently selected entity in the view.
    /// </summary>
    /// <returns>The selected entity, or null if no entity is selected.</returns>
    Entity? GetSelectedEntity();
}
