using System.Numerics;
using ECS;
using Editor.Features.Viewport.Gizmos;
using Engine.Renderer.Cameras;
using Engine.Scene.Components;

namespace Editor.Features.Viewport.Tools;

public class ScaleTool : IEntityTargetTool
{
    private Entity? _targetEntity;
    private GizmoAxis _activeAxis;
    private Vector2 _dragStartWorldPos;
    private Vector3 _dragStartScale;

    public EditorMode Mode => EditorMode.Scale;
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

        var hoveredAxis = GizmoRenderer.GetScaleHover(
            transform.Translation, viewportBounds, camera.GetViewProjectionMatrix(), mousePos);

        if (hoveredAxis == GizmoAxis.None) return;

        var worldPos = ViewportCoordinateConverter.ScreenToWorld2D(mousePos, viewportBounds, camera.GetViewProjectionMatrix());
        if (worldPos is null) return;

        _activeAxis = hoveredAxis;
        _dragStartWorldPos = worldPos.Value;
        _dragStartScale = transform.Scale;
    }

    public void OnMouseMove(Vector2 mousePos, Vector2[] viewportBounds, IViewCamera camera)
    {
        if (_activeAxis == GizmoAxis.None || _targetEntity == null) return;
        if (!_targetEntity.TryGetComponent<TransformComponent>(out var transform)) return;

        var currentWorld = ViewportCoordinateConverter.ScreenToWorld2D(mousePos, viewportBounds, camera.GetViewProjectionMatrix());
        if (currentWorld is null) return;

        var delta = currentWorld.Value - _dragStartWorldPos;

        transform.Scale = _activeAxis switch
        {
            GizmoAxis.X => _dragStartScale with
            {
                X = MathF.Max(0.01f, _dragStartScale.X * MathF.Max(0.01f, 1f + delta.X * 0.5f))
            },
            GizmoAxis.Y => _dragStartScale with
            {
                Y = MathF.Max(0.01f, _dragStartScale.Y * MathF.Max(0.01f, 1f + delta.Y * 0.5f))
            },
            _ => _dragStartScale * MathF.Max(0.01f, 1f + (delta.X + delta.Y) * 0.5f)
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

        var hover = GizmoRenderer.GetScaleHover(
            transform.Translation, viewportBounds, camera.GetViewProjectionMatrix(),
            ToLocal(ImGuiNET.ImGui.GetMousePos(), viewportBounds));

        GizmoRenderer.DrawScale(
            transform.Translation, viewportBounds, camera.GetViewProjectionMatrix(),
            _activeAxis != GizmoAxis.None ? _activeAxis : hover);
    }

    private static Vector2 ToLocal(Vector2 globalMouse, Vector2[] viewportBounds)
        => globalMouse - viewportBounds[0];
}
