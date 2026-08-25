using System.Numerics;
using ECS;
using Editor.Features.History;
using Editor.Features.History.Commands;
using Engine.Scene;
using Engine.Scene.Cameras;
using ImGuiNET;
using SceneComponents;

namespace Editor.Features.Viewport.Gizmos;

/// <summary>
/// Windows-only ImGuizmo tick. Immediate-mode: draw + drag live in Render.
/// </summary>
public static class ImGuizmoGizmo
{
    private static int _beginFrame = -1;
    private static bool _wasUsing;
    private static Vector3 _beforeT, _beforeR, _beforeS;

    public static bool IsAvailable => ImGuizmoNative.IsAvailable;
    public static bool IsUsing => IsAvailable && ImGuizmoNative.ImGuizmo_IsUsing();
    public static bool IsOver => IsAvailable && ImGuizmoNative.ImGuizmo_IsOver();

    public static bool TryRender(
        ImGuizmoOperation operation,
        TransformComponent transform,
        Entity entity,
        Vector2[] viewportBounds,
        IViewCamera camera,
        IEditorHistory history,
        ISceneContext sceneContext)
    {
        if (!IsAvailable)
            return false;

        var frame = ImGui.GetFrameCount();
        if (_beginFrame != frame)
        {
            _beginFrame = frame;
            ImGuizmoNative.ImGuizmo_BeginFrame();
        }

        var navigating = ImGui.GetIO().KeyAlt
            || ImGui.IsMouseDown(ImGuiMouseButton.Right)
            || ImGui.IsMouseDown(ImGuiMouseButton.Middle);
        ImGuizmoNative.ImGuizmo_Enable(!navigating);
        ImGuizmoNative.ImGuizmo_SetOrthographic(false);

        var origin = viewportBounds[0];
        var size = viewportBounds[1] - origin;
        ImGuizmoNative.ImGuizmo_SetRect(origin.X, origin.Y, size.X, size.Y);
        ImGuizmoNative.SetDrawlist(ImGui.GetWindowDrawList());

        var view = camera.GetViewMatrix();
        var projection = camera.GetProjectionMatrix();
        var world = transform.GetWorldTransform();

        ImGui.PushID(entity.Id);
        var changed = ImGuizmoNative.Manipulate(
            ref view, ref projection, (int)operation, (int)ImGuizmoMode.Local, ref world);
        ImGui.PopID();

        var usingNow = ImGuizmoNative.ImGuizmo_IsUsing();
        if (usingNow && !_wasUsing)
        {
            _beforeT = transform.Translation;
            _beforeR = transform.Rotation;
            _beforeS = transform.Scale;
        }

        if (changed)
            TransformGizmoMath.TryApplyWorldMatrix(transform, world);

        if (!usingNow && _wasUsing
            && sceneContext.ActiveScene is { } scene
            && !SetTransformCommand.TrsEqual(
                _beforeT, _beforeR, _beforeS,
                transform.Translation, transform.Rotation, transform.Scale))
        {
            history.Execute(new SetTransformCommand(
                scene, entity.Id,
                _beforeT, _beforeR, _beforeS,
                transform.Translation, transform.Rotation, transform.Scale));
        }

        _wasUsing = usingNow;
        return true;
    }
}
