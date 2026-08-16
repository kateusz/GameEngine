using System.Numerics;
using Audio;
using ECS;
using ECS.Systems;
using Input;
using SceneComponents;
using SceneComponents.Camera;
using SceneComponents.Rendering;
using Scripting;

namespace arena3d;

/// <summary>
/// WASD movement for the "player" entity, relative to the primary camera (RPG-style).
/// </summary>
[Register(typeof(IGameSystem))]
public class PlayerMovementSystem(IContext context, IKeyboardInput keyboard, IAudioPlayback audioPlayback) : IGameSystem
{
    private const float MoveSpeed = 2.0f;

    private bool _walkPlaying;

    public int Priority => 114;

    public void OnInit() { }

    public void OnShutdown()
    {
        var player = FindPlayer();
        if (player is null || !_walkPlaying)
            return;

        audioPlayback.Stop(player);
        _walkPlaying = false;
    }

    public void OnUpdate(TimeSpan deltaTime)
    {
        var player = FindPlayer();
        if (player is null || !player.TryGetComponent<TransformComponent>(out var transform))
            return;

        var moveDir = GetCameraRelativeMoveDir(player);
        if (moveDir == Vector3.Zero)
        {
            SyncMovement(player, false);
            return;
        }

        var dt = (float)deltaTime.TotalSeconds;
        transform.Translation += moveDir * MoveSpeed * dt;

        var yaw = MathF.Atan2(moveDir.X, moveDir.Z);
        transform.Rotation = new Vector3(transform.Rotation.X, yaw, transform.Rotation.Z);

        SyncMovement(player, true);
    }

    private Vector3 GetCameraRelativeMoveDir(Entity player)
    {
        if (!player.TryGetComponent<TransformComponent>(out var playerTransform))
            return Vector3.Zero;

        var cameraEntity = FindPrimaryCamera();
        if (cameraEntity is null || !cameraEntity.TryGetComponent<TransformComponent>(out var cameraTransform))
            return Vector3.Zero;

        var playerPos = playerTransform.Translation;
        var cameraPos = cameraTransform.Translation;

        var toPlayer = playerPos - cameraPos;
        var forward = new Vector3(toPlayer.X, 0f, toPlayer.Z);
        if (forward.LengthSquared() < 1e-8f)
            forward = Vector3.UnitZ;
        else
            forward = Vector3.Normalize(forward);

        var right = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, forward));

        var dir = Vector3.Zero;
        if (keyboard.IsKeyDown(KeyCodes.W)) dir += forward;
        if (keyboard.IsKeyDown(KeyCodes.S)) dir -= forward;
        if (keyboard.IsKeyDown(KeyCodes.D)) dir += right;
        if (keyboard.IsKeyDown(KeyCodes.A)) dir -= right;

        return dir == Vector3.Zero ? Vector3.Zero : Vector3.Normalize(dir);
    }

    private void SyncMovement(Entity player, bool moving)
    {
        SyncWalkAnimation(player, moving);
        SyncWalkSound(player, moving);
    }

    private void SyncWalkSound(Entity player, bool moving)
    {
        if (moving == _walkPlaying)
            return;

        if (moving)
            audioPlayback.Play(player);
        else
            audioPlayback.Pause(player);

        _walkPlaying = moving;
    }

    private static void SyncWalkAnimation(Entity player, bool moving)
    {
        if (!player.TryGetComponent<SkeletalPlaybackComponent>(out var skeletal))
            return;

        skeletal.Playing = moving;
    }

    private Entity? FindPrimaryCamera()
    {
        foreach (var (entity, camera) in context.View<CameraComponent>())
        {
            if (camera.Primary)
                return entity;
        }

        return null;
    }

    private Entity? FindPlayer()
    {
        foreach (var entity in context.Entities)
        {
            if (entity.Name == "player")
                return entity;
        }

        return null;
    }
}
