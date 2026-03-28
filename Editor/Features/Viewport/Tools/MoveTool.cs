using System.Numerics;
using ECS;
using Editor.Features.Viewport.Gizmos;
using Engine.Renderer.Cameras;
using Engine.Scene.Components;

namespace Editor.Features.Viewport.Tools;

public class MoveTool : IEntityTargetTool
{
    private Entity? _targetEntity;
    private GizmoAxis _activeAxis;
    private Vector2 _dragStartWorldPos;
    private Vector3 _dragStartEntityPos;

    public EditorMode Mode => EditorMode.Move;
    public bool IsActive => _activeAxis != GizmoAxis.None;

    public void SetTargetEntity(Entity? entity) => _targetEntity = entity;

    public void OnActivate() { }

    public void OnDeactivate()
    {
        _activeAxis = GizmoAxis.None;
        _targetEntity = null;
    }

    public void OnMouseDown(Vector2 mousePos, Vector2[] viewportBounds, IViewCamera camera)
    {
        if (_targetEntity == null || !_targetEntity.TryGetComponent<TransformComponent>(out var transform))
            return;

        var hoveredAxis = GizmoRenderer.GetTranslationHover(
            transform.Translation, viewportBounds, camera.GetViewProjectionMatrix(), mousePos);

        if (hoveredAxis == GizmoAxis.None) return;

        var worldPos = ViewportCoordinateConverter.ScreenToWorld2D(mousePos, viewportBounds, camera.GetViewProjectionMatrix());
        if (worldPos is null) return;

        _activeAxis = hoveredAxis;
        _dragStartWorldPos = worldPos.Value;
        _dragStartEntityPos = transform.Translation;
    }

    public void OnMouseMove(Vector2 mousePos, Vector2[] viewportBounds, IViewCamera camera)
    {
        if (_activeAxis == GizmoAxis.None || _targetEntity == null) return;
        if (!_targetEntity.TryGetComponent<TransformComponent>(out var transform)) return;

        var currentWorld = ViewportCoordinateConverter.ScreenToWorld2D(mousePos, viewportBounds, camera.GetViewProjectionMatrix());
        if (currentWorld is null) return;

        var delta = currentWorld.Value - _dragStartWorldPos;

        transform.Translation = _activeAxis switch
        {
            GizmoAxis.X => _dragStartEntityPos with { X = _dragStartEntityPos.X + delta.X },
            GizmoAxis.Y => _dragStartEntityPos with { Y = _dragStartEntityPos.Y + delta.Y },
            _ => new Vector3(_dragStartEntityPos.X + delta.X, _dragStartEntityPos.Y + delta.Y, _dragStartEntityPos.Z)
        };
    }

    public void OnMouseUp(Vector2 mousePos, Vector2[] viewportBounds, IViewCamera camera)
    {
        _activeAxis = GizmoAxis.None;
    }

    public void Render(Vector2[] viewportBounds, IViewCamera camera)
    {
        if (_targetEntity == null || !_targetEntity.TryGetComponent<TransformComponent>(out var transform))
            return;

        var hover = GizmoRenderer.GetTranslationHover(
            transform.Translation, viewportBounds, camera.GetViewProjectionMatrix(),
            ToLocal(ImGuiNET.ImGui.GetMousePos(), viewportBounds));

        GizmoRenderer.DrawTranslation(
            transform.Translation, viewportBounds, camera.GetViewProjectionMatrix(),
            _activeAxis != GizmoAxis.None ? _activeAxis : hover);
    }

    private static Vector2 ToLocal(Vector2 globalMouse, Vector2[] viewportBounds)
        => globalMouse - viewportBounds[0];
}
