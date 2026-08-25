using System.Numerics;
using SceneComponents;
using Math;

namespace Editor.Features.Viewport.Gizmos;

/// <summary>
/// Converts an ImGuizmo world matrix back into local TRS.
/// world = local * parentWorld (row-vector convention).
/// </summary>
public static class TransformGizmoMath
{
    public static bool TryApplyWorldMatrix(TransformComponent transform, Matrix4x4 world)
    {
        var localMat = transform.GetTransform();
        var worldMat = transform.GetWorldTransform();
        var parentWorld = Matrix4x4.Identity;
        if (Matrix4x4.Invert(localMat, out var invLocal))
            parentWorld = invLocal * worldMat;

        var local = world;
        if (Matrix4x4.Invert(parentWorld, out var invParent))
            local = world * invParent;

        if (!MathHelpers.DecomposeTransform(local, out var translation, out var rotation, out var scale))
            return false;

        transform.Translation = translation;
        transform.Rotation = rotation;
        transform.Scale = Vector3.Max(scale, new Vector3(0.01f));
        transform.SetWorldTransform(world);
        return true;
    }
}
