using System.Numerics;
using ECS;
using Engine.Renderer.Cameras;
using Engine.Scene.Components;

namespace Editor.Features.Viewport.Tools;

/// <summary>
/// Handles entity scaling in the editor viewport.
/// </summary>
public class ScaleTool : IEntityTargetTool, IEntityHoverTool
{
    private bool _isDragging;
    private Entity? _draggedEntity;
    private Vector2 _dragStartWorldPos;
    private Vector3 _dragStartScale;

    public EditorMode Mode => EditorMode.Scale;
    public bool IsActive => _isDragging;
    
    /// <summary>
    /// Currently hovered entity (set by EditorLayer from mouse picking).
    /// </summary>
    public Entity? HoveredEntity { get; set; }

    /// <summary>
    /// Sets the entity to manipulate.
    /// Called externally when an entity is selected.
    /// </summary>
    public void SetTargetEntity(Entity? entity)
    {
        if (!_isDragging)
        {
            _draggedEntity = entity;
        }
    }

    public void OnActivate()
    {
        // Tool activated - no special setup needed
    }

    public void OnDeactivate()
    {
        // Cancel any ongoing drag when switching modes
        if (_isDragging)
        {
            _isDragging = false;
            _draggedEntity = null;
        }
    }

    public void OnMouseDown(Vector2 mousePos, Vector2[] viewportBounds, OrthographicCamera camera)
    {
        // Only start scaling if we're hovering over the target entity
        if (_draggedEntity == null || HoveredEntity != _draggedEntity)
            return;

        _isDragging = true;
        _dragStartWorldPos = ViewportCoordinateConverter.ScreenToWorld(mousePos, viewportBounds, camera);

        if (_draggedEntity.TryGetComponent<TransformComponent>(out var transform))
        {
            _dragStartScale = transform.Scale;
        }
    }

    public void OnMouseMove(Vector2 mousePos, Vector2[] viewportBounds, OrthographicCamera camera)
    {
        if (!_isDragging || _draggedEntity == null)
            return;

        if (!_draggedEntity.TryGetComponent<TransformComponent>(out var transform))
            return;

        var currentWorldPos = ViewportCoordinateConverter.ScreenToWorld(mousePos, viewportBounds, camera);
        var delta = currentWorldPos - _dragStartWorldPos;

        // Scale based on mouse movement (both axes)
        var scaleFactor = 1.0f + (delta.X + delta.Y) * 0.5f; // Sensitivity factor
        scaleFactor = MathF.Max(0.1f, scaleFactor); // Prevent negative/zero scale
        transform.Scale = _dragStartScale * scaleFactor;
    }

    public void OnMouseUp(Vector2 mousePos, Vector2[] viewportBounds, OrthographicCamera camera)
    {
        _isDragging = false;
    }

    public void Render(Vector2[] viewportBounds, OrthographicCamera camera)
    {
        // ScaleTool doesn't render any overlays
        // Scale gizmos could be added here in the future
    }

    /// <summary>
    /// Checks if the given entity is currently being manipulated.
    /// </summary>
    public bool IsManipulating(Entity? entity)
    {
        return _isDragging && _draggedEntity == entity;
    }
}
