using ECS;

namespace Editor.Features.Viewport.Tools;

/// <summary>
/// Interface for tools that can manipulate a target entity (Move, Scale, etc.).
/// </summary>
public interface IEntityTargetTool : IViewportTool
{
    /// <summary>
    /// Sets the entity to manipulate.
    /// Called when entity selection changes.
    /// </summary>
    void SetTargetEntity(Entity? entity);
}

