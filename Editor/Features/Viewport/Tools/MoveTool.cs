using System.Numerics;
using ECS;
using Editor.Features.History;
using Editor.Features.History.Commands;
using Editor.Features.Viewport.Gizmos;
using Engine.Scene;
using Engine.Scene.Cameras;
using SceneComponents;

namespace Editor.Features.Viewport.Tools;

public class MoveTool(IEditorHistory history, ISceneContext sceneContext) : IEntityTargetTool
{
    private Entity? _targetEntity;
    private GizmoAxis _activeAxis;
    private Vector2 _dragStartWorldPos;
    private Vector3 _dragStartEntityPos;
    private Vector3 _dragStartRotation;
    private Vector3 _dragStartScale;

    public EditorMode Mode => EditorMode.Move;
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

        var hoveredAxis = GizmoRenderer.GetTranslationHover(
            worldPos, viewportBounds, camera.GetViewProjectionMatrix(), mousePos);

        if (hoveredAxis == GizmoAxis.None) return;

        var mouseWorld = ViewportCoordinateConverter.ScreenToWorld2D(mousePos, viewportBounds, camera.GetViewProjectionMatrix());
        if (mouseWorld is null) return;

        _activeAxis = hoveredAxis;
        _dragStartWorldPos = mouseWorld.Value;
        _dragStartEntityPos = transform.Translation; // edit local
        _dragStartRotation = transform.Rotation;
        _dragStartScale = transform.Scale;
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

        transform.Translation = _activeAxis switch
        {
            GizmoAxis.X => _dragStartEntityPos with { X = _dragStartEntityPos.X + deltaLocal.X },
            GizmoAxis.Y => _dragStartEntityPos with { Y = _dragStartEntityPos.Y + deltaLocal.Y },
            _ => new Vector3(_dragStartEntityPos.X + deltaLocal.X, _dragStartEntityPos.Y + deltaLocal.Y, _dragStartEntityPos.Z)
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
                _dragStartEntityPos, _dragStartRotation, _dragStartScale,
                transform.Translation, transform.Rotation, transform.Scale))
        {
            history.Execute(new SetTransformCommand(
                scene, _targetEntity.Id,
                _dragStartEntityPos, _dragStartRotation, _dragStartScale,
                transform.Translation, transform.Rotation, transform.Scale));
        }

        _activeAxis = GizmoAxis.None;
    }

    public void Render(Vector2[] viewportBounds, IViewCamera camera)
    {
        if (_targetEntity == null || !_targetEntity.TryGetComponent<TransformComponent>(out var transform))
            return;

        if (ImGuizmoGizmo.TryRender(
                ImGuizmoOperation.Translate, transform, _targetEntity,
                viewportBounds, camera, history, sceneContext))
            return;

        var worldPos = transform.GetWorldTransform().Translation;
        var hover = GizmoRenderer.GetTranslationHover(
            worldPos, viewportBounds, camera.GetViewProjectionMatrix(),
            ToLocal(ImGuiNET.ImGui.GetMousePos(), viewportBounds));

        GizmoRenderer.DrawTranslation(
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
