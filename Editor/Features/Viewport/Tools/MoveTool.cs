using System.Diagnostics;
using System.Numerics;
using ECS;
using Editor.Features.Viewport.Gizmos;
using Engine.Scene;
using Engine.Scene.Cameras;
using ImGuiNET;
using SceneComponents;

namespace Editor.Features.Viewport.Tools;

/// <summary>
/// Move gizmo: free path writes local = startLocal + worldDelta (W3 — parented free Move unchanged).
/// Snap-on path: propose world → axis mask → SnapWorldPosition → world→local Translation.
/// </summary>
public class MoveTool(IViewportSnapService snapService, ISceneContext sceneContext) : IEntityTargetTool
{
    private Entity? _targetEntity;
    private GizmoAxis _activeAxis;
    private Vector2 _dragStartWorldPos;
    private Vector3 _dragStartEntityPos;
    private Vector3 _dragStartEntityWorldPos;

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

        var worldPos = transform.GetWorldTransform().Translation;

        var hoveredAxis = GizmoRenderer.GetTranslationHover(
            worldPos, viewportBounds, camera.GetViewProjectionMatrix(), mousePos);

        if (hoveredAxis == GizmoAxis.None) return;

        var mouseWorld = ViewportCoordinateConverter.ScreenToWorld2D(mousePos, viewportBounds, camera.GetViewProjectionMatrix());
        if (mouseWorld is null) return;

        _activeAxis = hoveredAxis;
        _dragStartWorldPos = mouseWorld.Value;
        _dragStartEntityPos = transform.Translation; // free path: edit local (W3)
        _dragStartEntityWorldPos = worldPos; // snap path: world lattice basis (W2)
    }

    public void OnMouseMove(Vector2 mousePos, Vector2[] viewportBounds, IViewCamera camera)
    {
        if (_activeAxis == GizmoAxis.None || _targetEntity == null) return;
        if (!_targetEntity.TryGetComponent<TransformComponent>(out var transform)) return;

        var currentWorld = ViewportCoordinateConverter.ScreenToWorld2D(mousePos, viewportBounds, camera.GetViewProjectionMatrix());
        if (currentWorld is null) return;

        var delta = currentWorld.Value - _dragStartWorldPos;

        var bypassHeld = ImGui.GetIO().KeyCtrl || ImGui.GetIO().KeySuper;
        if (snapService.ShouldSnap(bypassHeld))
        {
            var proposedWorld = ProposeWorld(_dragStartEntityWorldPos, delta, _activeAxis);
            var (snapX, snapY) = AxisSnapMask(_activeAxis);
            var snappedWorld = snapService.SnapWorldPosition(proposedWorld, snapX, snapY);
            // Invert failure → keep prior local (do not write world-as-local)
            if (TryWorldToLocalTranslation(snappedWorld, _targetEntity, transform.Translation, out var local))
                transform.Translation = local;
            return;
        }

        // Free path — keep existing local = startLocal + worldDelta (W3: do not rewrite for parents)
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

        var worldPos = transform.GetWorldTransform().Translation;
        var hover = GizmoRenderer.GetTranslationHover(
            worldPos, viewportBounds, camera.GetViewProjectionMatrix(),
            ToLocal(ImGui.GetMousePos(), viewportBounds));

        GizmoRenderer.DrawTranslation(
            worldPos, viewportBounds, camera.GetViewProjectionMatrix(),
            _activeAxis != GizmoAxis.None ? _activeAxis : hover);
    }

    private bool TryWorldToLocalTranslation(Vector3 worldPos, Entity entity, Vector3 priorLocal, out Vector3 local)
    {
        var parent = sceneContext.ActiveScene?.GetParent(entity);
        if (parent is null || !parent.TryGetComponent<TransformComponent>(out var parentTransform))
        {
            local = new Vector3(worldPos.X, worldPos.Y, priorLocal.Z);
            return true;
        }

        return TryWorldToLocalWithParent(worldPos, parentTransform.GetWorldTransform(), priorLocal, out local);
    }

    /// <summary>Row-vector: world = local * parentWorld ⇒ local = world * inv(parentWorld).</summary>
    internal static bool TryWorldToLocalWithParent(Vector3 worldPos, Matrix4x4 parentWorld, Vector3 priorLocal, out Vector3 local)
    {
        if (!Matrix4x4.Invert(parentWorld, out var invParent))
        {
            local = priorLocal;
            return false;
        }

        var transformed = Vector3.Transform(worldPos, invParent);
        local = transformed with { Z = priorLocal.Z };
        return true;
    }

    private static Vector3 ProposeWorld(Vector3 worldStart, Vector2 delta, GizmoAxis axis) => axis switch
    {
        GizmoAxis.X => worldStart with { X = worldStart.X + delta.X },
        GizmoAxis.Y => worldStart with { Y = worldStart.Y + delta.Y },
        _ => worldStart with { X = worldStart.X + delta.X, Y = worldStart.Y + delta.Y }
    };

    private static (bool snapX, bool snapY) AxisSnapMask(GizmoAxis axis) => axis switch
    {
        GizmoAxis.X => (true, false),
        GizmoAxis.Y => (false, true),
        _ => (true, true) // Free ≡ XY
    };

    private static Vector2 ToLocal(Vector2 globalMouse, Vector2[] viewportBounds)
        => globalMouse - viewportBounds[0];

    /// <summary>World→local asserts (D3A). Hosted once from EditorLifecycle in Debug.</summary>
    [Conditional("DEBUG")]
    public static void SelfCheck()
    {
        var prior = new Vector3(0f, 0f, 1.25f);

        // Root / identity parent
        Debug.Assert(TryWorldToLocalWithParent(new Vector3(3.5f, -2.25f, 9f), Matrix4x4.Identity, prior, out var root));
        Debug.Assert(root.X == 3.5f && root.Y == -2.25f && root.Z == 1.25f,
            "MoveTool: identity parent must map world XY→local XY and preserve Z");

        // Translated parent
        var translatedParent = Matrix4x4.CreateTranslation(10f, 5f, 0f);
        Debug.Assert(TryWorldToLocalWithParent(new Vector3(12f, 7f, 0f), translatedParent, prior with { Z = 0.5f }, out var child));
        Debug.Assert(System.MathF.Abs(child.X - 2f) < 1e-5f && System.MathF.Abs(child.Y - 2f) < 1e-5f && child.Z == 0.5f,
            "MoveTool: translated parent inverse must yield local = world - parentT");

        // Rotated parent (90° around Z) — round-trip world → local → world
        var rotatedParent = Matrix4x4.CreateRotationZ(System.MathF.PI / 2f);
        var rotatedWorld = new Vector3(0f, 1f, 0f);
        Debug.Assert(TryWorldToLocalWithParent(rotatedWorld, rotatedParent, prior with { Z = 0.25f }, out var rotatedLocal));
        Debug.Assert(rotatedLocal.Z == 0.25f, "MoveTool: rotated parent must preserve Z");
        var rotatedRoundTrip = Vector3.Transform(rotatedLocal with { Z = 0f }, rotatedParent);
        Debug.Assert(System.MathF.Abs(rotatedRoundTrip.X - rotatedWorld.X) < 1e-4f
                     && System.MathF.Abs(rotatedRoundTrip.Y - rotatedWorld.Y) < 1e-4f,
            "MoveTool: rotated local*parentWorld must recover world XY");

        // Scaled + translated parent round-trip
        var scaledParent = Matrix4x4.CreateScale(2f, 2f, 1f) * Matrix4x4.CreateTranslation(4f, 0f, 0f);
        Debug.Assert(TryWorldToLocalWithParent(new Vector3(8f, 2f, 0f), scaledParent, prior with { Z = 0.1f }, out var scaledLocal));
        var scaledWorldAgain = Vector3.Transform(scaledLocal with { Z = 0f }, scaledParent);
        Debug.Assert(System.MathF.Abs(scaledWorldAgain.X - 8f) < 1e-3f && System.MathF.Abs(scaledWorldAgain.Y - 2f) < 1e-3f,
            "MoveTool: scaled+translated parent round-trip must recover world XY");
        Debug.Assert(scaledLocal.Z == 0.1f, "MoveTool: scaled parent must preserve Z");

        // Singular / non-invertible → keep prior local, return false
        var singular = new Matrix4x4(
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 1);
        var kept = new Vector3(1f, 2f, 3f);
        Debug.Assert(!TryWorldToLocalWithParent(new Vector3(9f, 9f, 9f), singular, kept, out var fallback));
        Debug.Assert(fallback == kept, "MoveTool: invert failure must keep prior local");
    }
}
