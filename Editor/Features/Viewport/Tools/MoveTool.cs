using System.Numerics;
using ECS;
using Engine.Renderer.Cameras;
using Engine.Scene.Components;

namespace Editor.Features.Viewport.Tools;

/// <summary>
/// Handles entity movement in the editor viewport.
/// </summary>
public class MoveTool : IEntityTargetTool, IEntityHoverTool
{
    private Entity? _draggedEntity;
    private Vector2 _dragStartWorldPos;
    private Vector3 _dragStartEntityPos;

    public EditorMode Mode => EditorMode.Move;
    public bool IsActive { get; private set; }
    
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
        if (!IsActive) 
            _draggedEntity = entity;
    }

    public void OnActivate()
    {
    }

    public void OnDeactivate()
    {
        if (IsActive)
        {
            IsActive = false;
            _draggedEntity = null;
        }
    }

    public void OnMouseDown(Vector2 mousePos, Vector2[] viewportBounds, OrthographicCamera camera)
    {
        // Only start dragging if we're hovering over the target entity
        if (_draggedEntity == null || HoveredEntity != _draggedEntity)
            return;

        IsActive = true;
        _dragStartWorldPos = ViewportCoordinateConverter.ScreenToWorld(mousePos, viewportBounds, camera);

        if (_draggedEntity.TryGetComponent<TransformComponent>(out var transform))
        {
            _dragStartEntityPos = transform.Translation;
        }
    }

    public void OnMouseMove(Vector2 mousePos, Vector2[] viewportBounds, OrthographicCamera camera)
    {
        if (!IsActive || _draggedEntity == null)
            return;

        if (!_draggedEntity.TryGetComponent<TransformComponent>(out var transform))
            return;

        var currentWorldPos = ViewportCoordinateConverter.ScreenToWorld(mousePos, viewportBounds, camera);
        var delta = currentWorldPos - _dragStartWorldPos;

        // Update entity position
        transform.Translation = new Vector3(
            _dragStartEntityPos.X + delta.X,
            _dragStartEntityPos.Y + delta.Y,
            _dragStartEntityPos.Z
        );
    }

    public void OnMouseUp(Vector2 mousePos, Vector2[] viewportBounds, OrthographicCamera camera)
    {
        IsActive = false;
    }

    public void Render(Vector2[] viewportBounds, OrthographicCamera camera)
    {
        // TODO: add Gizmos here
    }
}
