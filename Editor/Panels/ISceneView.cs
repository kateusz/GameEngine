using ECS;
using Engine.Scene;

namespace Editor.Panels;

/// <summary>
/// Abstraction for scene view components that display and interact with scenes.
/// Decouples SceneManager from specific UI implementations.
/// </summary>
/// <remarks>
/// Implementations are responsible for:
/// - Managing scene context lifecycle
/// - Tracking entity selection state and notifying subscribers via SelectionChanged event
/// - Rendering UI (for UI-based implementations)
///
/// Example usage:
/// <code>
/// var sceneManager = new SceneManager(sceneView, serializer);
/// sceneView.SelectionChanged += (entity) => Console.WriteLine($"Selected: {entity?.Name}");
/// sceneManager.New(viewportSize);  // Calls sceneView.SetContext()
/// </code>
/// </remarks>
public interface ISceneView
{
    /// <summary>
    /// Event raised when the selected entity changes.
    /// </summary>
    /// <remarks>
    /// The event argument is the newly selected entity, or null if the selection was cleared.
    /// Subscribers should handle null values appropriately.
    /// </remarks>
    event Action<Entity?>? SelectionChanged;

    /// <summary>
    /// Sets the current scene context for the view.
    /// </summary>
    /// <param name="context">The scene to display, or null to clear the context.</param>
    void SetContext(Scene? context);

    /// <summary>
    /// Gets the currently selected entity in the view.
    /// </summary>
    /// <returns>The selected entity, or null if no entity is selected.</returns>
    /// <remarks>
    /// Prefer subscribing to SelectionChanged event for reactive notification instead of polling this property.
    /// </remarks>
    Entity? GetSelectedEntity();
}
