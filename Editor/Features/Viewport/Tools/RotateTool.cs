using System.Numerics;
using ECS;
using Editor.Features.History;
using Editor.Features.History.Commands;
using Editor.Features.Viewport.Gizmos;
using Engine.Scene;
using Engine.Scene.Cameras;
using SceneComponents;

namespace Editor.Features.Viewport.Tools;

public class RotateTool(IEditorHistory history, ISceneContext sceneContext) : IEntityTargetTool
{
    private Entity? _targetEntity;
    private bool _isRotating;
    private float _startAngle;
    private float _startRotZ;
    private Vector3 _dragStartTranslation;
    private Vector3 _dragStartRotation;
    private Vector3 _dragStartScale;

    public EditorMode Mode => EditorMode.Rotate;
    public bool IsActive => _isRotating;

    public void SetTargetEntity(Entity? entity) => _targetEntity = entity;

    public void OnActivate() { }

    public void OnDeactivate()
    {
        _isRotating = false;
        _targetEntity = null;
    }

    public void OnMouseDown(Vector2 mousePos, Vector2[] viewportBounds, IViewCamera camera)
    {
        if (_targetEntity == null || !_targetEntity.TryGetComponent<TransformComponent>(out var transform))
            return;

        var worldPos = transform.GetWorldTransform().Translation;

        if (!GizmoRenderer.GetRotationHover(
                worldPos, viewportBounds, camera.GetViewProjectionMatrix(), mousePos))
            return;

        var origin = ViewportCoordinateConverter.WorldToScreen(
            worldPos, viewportBounds, camera.GetViewProjectionMatrix());
        var globalMouse = mousePos + viewportBounds[0];

        _isRotating = true;
        _startAngle = MathF.Atan2(globalMouse.Y - origin.Y, globalMouse.X - origin.X);
        _startRotZ = transform.Rotation.Z;
        _dragStartTranslation = transform.Translation;
        _dragStartRotation = transform.Rotation;
        _dragStartScale = transform.Scale;
    }

    public void OnMouseMove(Vector2 mousePos, Vector2[] viewportBounds, IViewCamera camera)
    {
        if (!_isRotating || _targetEntity == null) return;
        if (!_targetEntity.TryGetComponent<TransformComponent>(out var transform)) return;

        var worldPos = transform.GetWorldTransform().Translation;
        var origin = ViewportCoordinateConverter.WorldToScreen(
            worldPos, viewportBounds, camera.GetViewProjectionMatrix());
        var globalMouse = mousePos + viewportBounds[0];

        var currentAngle = MathF.Atan2(globalMouse.Y - origin.Y, globalMouse.X - origin.X);
        var delta = currentAngle - _startAngle;

        transform.Rotation = transform.Rotation with { Z = _startRotZ - delta };
    }

    public void OnMouseUp(Vector2 mousePos, Vector2[] viewportBounds, IViewCamera camera)
    {
        if (_isRotating
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

        _isRotating = false;
    }

    public void Render(Vector2[] viewportBounds, IViewCamera camera)
    {
        if (_targetEntity == null || !_targetEntity.TryGetComponent<TransformComponent>(out var transform))
            return;

        var worldPos = transform.GetWorldTransform().Translation;
        var localMouse = ImGuiNET.ImGui.GetMousePos() - viewportBounds[0];
        var hover = _isRotating || GizmoRenderer.GetRotationHover(
            worldPos, viewportBounds, camera.GetViewProjectionMatrix(), localMouse);

        GizmoRenderer.DrawRotation(
            worldPos, transform.Rotation.Z,
            viewportBounds, camera.GetViewProjectionMatrix(), hover);
    }
}
