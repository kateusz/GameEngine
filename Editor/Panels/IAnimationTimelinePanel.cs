using ECS;

namespace Editor.Panels;

/// <summary>
/// Interface for the Animation Timeline panel.
/// Provides visual authoring tools for animators with frame-by-frame control.
/// </summary>
public interface IAnimationTimelinePanel
{
    /// <summary>
    /// Sets the entity to edit animations for.
    /// </summary>
    /// <param name="entity">The entity with AnimationComponent to edit.</param>
    void SetEntity(Entity entity);

    /// <summary>
    /// Updates the animation preview state.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last update in seconds.</param>
    void Update(float deltaTime);

    /// <summary>
    /// Renders the panel using ImGui.
    /// </summary>
    /// <param name="viewportDockId">Optional dock ID for initial docking to viewport.</param>
    void OnImGuiRender(uint viewportDockId = 0);
}
