using ECS;
using ECS.Systems;
using Input;
using SceneComponents;
using SceneComponents.Camera;
using Scripting;

namespace IslandDash;

/// <summary>
/// Third-person follow camera: behind/above the player, smoothed follow (slower vertically), look-ahead down the
/// view direction, spring-arm pulled in by level geometry, and per-section overrides from level camera zones.
/// </summary>
[Register(typeof(IGameSystem))]
public class CameraSystem(Context context, IKeyboardInput keyboard, IMouseInput mouse) : IGameSystem
{
    private const float ZoneProbeHeight = 0.5f;

    public int Priority => SystemOrder.Camera;

    public void OnInit() { }

    public void OnShutdown() { }

    public void OnUpdate(TimeSpan deltaTime)
    {
        if (World.State(context) is not { } state || World.PrimaryCamera(context) is not { } cameraEntity ||
            World.Player(context) is not { } player)
            return;

        var dt = SystemOrder.ClampDelta(deltaTime);
        var tuning = World.Get<CameraTuningComponent>(context);
        if (!cameraEntity.TryGetComponent<CameraRigComponent>(out var rig))
        {
            rig = cameraEntity.AddComponent(new CameraRigComponent
            {
                Yaw = tuning.Yaw, Pitch = tuning.Pitch, Distance = tuning.Distance,
                FocusHeight = tuning.FocusHeight, ArmLength = tuning.Distance
            });
        }

        cameraEntity.GetComponent<CameraComponent>().CameraViewTransform = null;
        var snap = state.CameraSnap;
        // Keep following a death so the fall stays on screen. Pause freezes the view.
        if (!snap && state.Phase == GamePhase.Paused)
            return;

        var playerPosition = player.GetComponent<TransformComponent>().Translation;
        var zone = FindZone(playerPosition + new Vector3(0f, ZoneProbeHeight, 0f));
        BlendSettings(rig, tuning, zone, snap, dt);
        Orbit(rig, tuning, zone, state, dt);

        var yaw = rig.Yaw + rig.UserYaw;
        var orientation = Quaternion.CreateFromYawPitchRoll(yaw, rig.Pitch, 0f);
        var flatForward = Vector3.Transform(-Vector3.UnitZ, GameMath.YawRotation(yaw));
        var desiredFocus = playerPosition + new Vector3(0f, rig.FocusHeight, 0f) + flatForward * tuning.LookAhead;
        var verticalSharpness = state.Phase == GamePhase.Dying ? 8f : tuning.VerticalSharpness;
        rig.Focus = snap
            ? desiredFocus
            : new Vector3(
                GameMath.Damp(rig.Focus.X, desiredFocus.X, tuning.FollowSharpness, dt),
                GameMath.Damp(rig.Focus.Y, desiredFocus.Y, verticalSharpness, dt),
                GameMath.Damp(rig.Focus.Z, desiredFocus.Z, tuning.FollowSharpness, dt));

        var back = Vector3.Transform(Vector3.UnitZ, orientation);
        var hit = Collision.Raycast(context, rig.Focus, back, rig.Distance + tuning.CollisionMargin);
        var allowed = System.Math.Clamp(hit - tuning.CollisionMargin, tuning.MinDistance, rig.Distance);
        rig.ArmLength = snap || allowed < rig.ArmLength
            ? allowed
            : GameMath.Damp(rig.ArmLength, allowed, tuning.ArmReturnSharpness, dt);

        var transform = cameraEntity.GetComponent<TransformComponent>();
        transform.Translation = rig.Focus + back * rig.ArmLength;
        transform.Rotation = GameMath.ToEngineEuler(orientation);
        rig.Orientation = orientation;

        state.CameraYaw = yaw;
        state.CameraSnap = false;
    }

    private static void BlendSettings(CameraRigComponent rig, CameraTuningComponent tuning, CameraZoneComponent? zone, bool snap, float dt)
    {
        var yaw = zone?.Yaw ?? tuning.Yaw;
        var pitch = zone?.Pitch ?? tuning.Pitch;
        var distance = zone?.Distance ?? tuning.Distance;
        var height = zone?.FocusHeight ?? tuning.FocusHeight;
        if (snap)
        {
            rig.Yaw = yaw;
            rig.Pitch = pitch;
            rig.Distance = distance;
            rig.FocusHeight = height;
            return;
        }

        var k = tuning.ZoneBlendSharpness;
        rig.Yaw = GameMath.DampAngle(rig.Yaw, yaw, k, dt);
        rig.Pitch = GameMath.Damp(rig.Pitch, pitch, k, dt);
        rig.Distance = GameMath.Damp(rig.Distance, distance, k, dt);
        rig.FocusHeight = GameMath.Damp(rig.FocusHeight, height, k, dt);
    }

    private void Orbit(CameraRigComponent rig, CameraTuningComponent tuning, CameraZoneComponent? zone, GameStateComponent state, float dt)
    {
        if (zone is { Fixed: true } || state.Phase != GamePhase.Playing)
        {
            rig.UserYaw = GameMath.DampAngle(rig.UserYaw, 0f, tuning.ZoneBlendSharpness, dt);
            return;
        }

        var input = GameInput.Read(keyboard, mouse);
        rig.UserYaw += input.OrbitAxis * tuning.OrbitSpeed * dt;
        if (input.OrbitDrag)
            rig.UserYaw -= input.MouseDelta.X * tuning.MouseOrbitSensitivity;
    }

    /// <summary>Smallest zone containing the point, so nested zones override their surroundings.</summary>
    private CameraZoneComponent? FindZone(Vector3 point)
    {
        CameraZoneComponent? best = null;
        var bestVolume = float.MaxValue;
        foreach (var (_, zone) in context.View<CameraZoneComponent>())
        {
            if (!zone.Bounds.Contains(point))
                continue;
            var size = zone.Bounds.Max - zone.Bounds.Min;
            var volume = size.X * size.Y * size.Z;
            if (volume < bestVolume)
            {
                best = zone;
                bestVolume = volume;
            }
        }

        return best;
    }
}
