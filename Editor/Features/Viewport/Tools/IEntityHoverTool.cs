using ECS;

namespace Editor.Features.Viewport.Tools;

/// <summary>
/// Interface for tools that track hovered entities (Selection tool, etc.).
/// </summary>
public interface IEntityHoverTool : IViewportTool
{
    /// <summary>
    /// Gets or sets the currently hovered entity.
    /// Updated by EditorLayer based on mouse picking.
    /// </summary>
    Entity? HoveredEntity { get; set; }
}

