using System.Numerics;
using ECS;
using Editor.Features.History;
using Editor.Features.History.Commands;
using Editor.Features.Viewport.Gizmos;
using Engine.Scene;
using Engine.Scene.Cameras;
using SceneComponents;

namespace Editor.Features.Viewport.Tools;

public class ScaleTool(IEditorHistory history, ISceneContext sceneContext) : IEntityTargetTool
{
    private Entity? _targetEntity;
    private GizmoAxis _activeAxis;
    private Vector2 _dragStartWorldPos;
    private Vector3 _dragStartScale;
    private Vector3 _dragStartTranslation;
    private Vector3 _dragStartRotation;

    public EditorMode Mode => EditorMode.Scale;
    public bool IsActive => ImGuizmoGizmo.IsAvailable ? ImGuizmoGizmo.IsUsing : _activeAxis != GizmoAxis.None;

    public void SetTargetEntity(Entity? entity) => _targetEntity = entity;

    public void OnActivate() { }

    public void OnDeactivate()
    {
        _activeAxis = GizmoAxis.None;
        _targetEntity = null;
    }

    public void OnMouseDown(Vector2 mousePos, Vector2[] viewportBounds, IViewCamera camera)
    {
        if (ImGuizmoGizmo.IsAvailable)
            return;
        if (_targetEntity == null || !_targetEntity.TryGetComponent<TransformComponent>(out var transform))
            return;

        var worldPos = transform.GetWorldTransform().Translation;
        var hoveredAxis = GizmoRenderer.GetScaleHover(
            worldPos, viewportBounds, camera.GetViewProjectionMatrix(), mousePos);

        if (hoveredAxis == GizmoAxis.None) return;

        var mouseWorld = ViewportCoordinateConverter.ScreenToWorld2D(mousePos, viewportBounds, camera.GetViewProjectionMatrix());
        if (mouseWorld is null) return;

        _activeAxis = hoveredAxis;
        _dragStartWorldPos = mouseWorld.Value;
        _dragStartScale = transform.Scale;
        _dragStartTranslation = transform.Translation;
        _dragStartRotation = transform.Rotation;
    }

    public void OnMouseMove(Vector2 mousePos, Vector2[] viewportBounds, IViewCamera camera)
    {
        if (ImGuizmoGizmo.IsAvailable)
            return;
        if (_activeAxis == GizmoAxis.None || _targetEntity == null) return;
        if (!_targetEntity.TryGetComponent<TransformComponent>(out var transform)) return;

        var currentWorld = ViewportCoordinateConverter.ScreenToWorld2D(mousePos, viewportBounds, camera.GetViewProjectionMatrix());
        if (currentWorld is null) return;

        var delta = currentWorld.Value - _dragStartWorldPos;
        var deltaLocal = WorldDeltaToLocal(transform, new Vector3(delta.X, delta.Y, 0f));

        transform.Scale = _activeAxis switch
        {
            GizmoAxis.X => _dragStartScale with
            {
                X = MathF.Max(0.01f, _dragStartScale.X * MathF.Max(0.01f, 1f + deltaLocal.X * 0.5f))
            },
            GizmoAxis.Y => _dragStartScale with
            {
                Y = MathF.Max(0.01f, _dragStartScale.Y * MathF.Max(0.01f, 1f + deltaLocal.Y * 0.5f))
            },
            _ => _dragStartScale * MathF.Max(0.01f, 1f + (deltaLocal.X + deltaLocal.Y) * 0.5f)
        };
    }

    public void OnMouseUp(Vector2 mousePos, Vector2[] viewportBounds, IViewCamera camera)
    {
        if (ImGuizmoGizmo.IsAvailable)
            return;
        if (_activeAxis != GizmoAxis.None
            && _targetEntity is not null
            && _targetEntity.TryGetComponent<TransformComponent>(out var transform)
            && sceneContext.ActiveScene is { } scene
            && !SetTransformCommand.TrsEqual(
                _dragStartTranslation, _dragStartRotation, _dragStartScale,
                transform.Translation, transform.Rotation, transform.Scale))
        {
            history.Execute(new SetTransformCommand(
                scene, _targetEntity.Id,
                _dragStartTranslation, _dragStartRotation, _dragStartScale,
                transform.Translation, transform.Rotation, transform.Scale));
        }

        _activeAxis = GizmoAxis.None;
    }

    public void Render(Vector2[] viewportBounds, IViewCamera camera)
    {
        if (_targetEntity == null || !_targetEntity.TryGetComponent<TransformComponent>(out var transform))
            return;

        if (ImGuizmoGizmo.TryRender(
                ImGuizmoOperation.Scale, transform, _targetEntity,
                viewportBounds, camera, history, sceneContext))
            return;

        var worldPos = transform.GetWorldTransform().Translation;
        var hover = GizmoRenderer.GetScaleHover(
            worldPos, viewportBounds, camera.GetViewProjectionMatrix(),
            ToLocal(ImGuiNET.ImGui.GetMousePos(), viewportBounds));

        GizmoRenderer.DrawScale(
            worldPos, viewportBounds, camera.GetViewProjectionMatrix(),
            _activeAxis != GizmoAxis.None ? _activeAxis : hover);
    }

    /// <summary>
    /// world = local * parentWorld ⇒ parentWorld = inv(local) * world.
    /// Maps a world-space drag delta into the entity's parent-local space.
    /// </summary>
    private static Vector3 WorldDeltaToLocal(TransformComponent transform, Vector3 deltaWorld)
    {
        var local = transform.GetTransform();
        var world = transform.GetWorldTransform();
        if (!Matrix4x4.Invert(local, out var invLocal))
            return deltaWorld;

        var parentWorld = invLocal * world;
        if (!Matrix4x4.Invert(parentWorld, out var invParent))
            return deltaWorld;

        return Vector3.TransformNormal(deltaWorld, invParent);
    }

    private static Vector2 ToLocal(Vector2 globalMouse, Vector2[] viewportBounds)
        => globalMouse - viewportBounds[0];
}
