using System.Numerics;
using ECS;
using Editor.Utilities;
using Engine.Renderer.Cameras;
using Engine.Scene.Components;

namespace Editor;

/// <summary>
/// Handles object manipulation in the editor viewport (move, rotate, scale).
/// </summary>
public class ObjectManipulator
{
    private bool _isDragging;
    private Entity? _draggedEntity;
    private Vector2 _dragStartWorldPos;
    private Vector3 _dragStartEntityPos;
    private Vector3 _dragStartScale;

    public bool IsDragging => _isDragging;

    /// <summary>
    /// Starts manipulating an entity.
    /// </summary>
    /// <param name="entity">Entity to manipulate</param>
    /// <param name="mousePos">Current mouse position in screen space</param>
    /// <param name="viewportBounds">Viewport bounds [min, max]</param>
    /// <param name="camera">Orthographic camera</param>
    public void StartDrag(Entity entity, Vector2 mousePos, Vector2[] viewportBounds, OrthographicCamera camera)
    {
        _isDragging = true;
        _draggedEntity = entity;
        
        _dragStartWorldPos = ViewportCoordinateConverter.ScreenToWorld(mousePos, viewportBounds, camera);
        
        if (_draggedEntity.TryGetComponent<TransformComponent>(out var transform))
        {
            _dragStartEntityPos = transform.Translation;
            _dragStartScale = transform.Scale;
        }
    }

    /// <summary>
    /// Updates the manipulation based on current mouse position and editor mode.
    /// </summary>
    /// <param name="mode">Current editor mode</param>
    /// <param name="mousePos">Current mouse position in screen space</param>
    /// <param name="viewportBounds">Viewport bounds [min, max]</param>
    /// <param name="camera">Orthographic camera</param>
    public void UpdateDrag(EditorMode mode, Vector2 mousePos, Vector2[] viewportBounds, OrthographicCamera camera)
    {
        if (!_isDragging || _draggedEntity == null)
            return;

        if (!_draggedEntity.TryGetComponent<TransformComponent>(out var transform))
            return;

        var currentWorldPos = ViewportCoordinateConverter.ScreenToWorld(mousePos, viewportBounds, camera);
        var delta = currentWorldPos - _dragStartWorldPos;

        switch (mode)
        {
            case EditorMode.Move:
                // Update entity position
                transform.Translation = new Vector3(
                    _dragStartEntityPos.X + delta.X,
                    _dragStartEntityPos.Y + delta.Y,
                    _dragStartEntityPos.Z
                );
                break;

            case EditorMode.Scale:
                // Scale based on mouse movement (both axes)
                var scaleFactor = 1.0f + (delta.X + delta.Y) * 0.5f; // Sensitivity factor
                scaleFactor = MathF.Max(0.1f, scaleFactor); // Prevent negative/zero scale
                transform.Scale = _dragStartScale * scaleFactor;
                break;
        }
    }

    /// <summary>
    /// Ends the current manipulation operation.
    /// </summary>
    public void EndDrag()
    {
        _isDragging = false;
        _draggedEntity = null;
    }

    /// <summary>
    /// Checks if the given entity is currently being manipulated.
    /// </summary>
    public bool IsManipulating(Entity? entity)
    {
        return _isDragging && _draggedEntity == entity;
    }
}


