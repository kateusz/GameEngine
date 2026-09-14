using System.Numerics;
using ECS;
using ECS.Systems;
using Input;
using SceneComponents;
using SceneComponents.Camera;
using Scripting;

namespace arena3d;

/// <summary>
/// Third-person orbit camera behind the "player" entity
/// Hold RMB to rotate; camera always looks at the player.
/// </summary>
[Register(typeof(IGameSystem))]
public class PlayerFollowCameraSystem(IContext context, IMouseInput mouse, ICameraQueries cameraQueries) : IGameSystem
{
    private const float FollowDistance = 2.2f;
    private const float LookAtHeight = 0.2f;
    private const float MouseSensitivity = 0.003f;
    private const float MinPitch = -0.85f;
    private const float MaxPitch = 0.85f;

    private Entity? _cameraEntity;
    private float _yaw;
    private float _pitch = 0.12f;

    public int Priority => 140;

    public void OnInit()
    {
        foreach (var (entity, camera) in context.View<CameraComponent>())
        {
            if (camera.Primary)
            {
                _cameraEntity = entity;
                break;
            }
        }
    }

    public void OnShutdown() { }

    public void OnUpdate(TimeSpan deltaTime)
    {
        if (_cameraEntity is null)
            return;

        var player = FindPlayer();
        if (player is null)
            return;

        if (!player.TryGetComponent<TransformComponent>(out var playerTransform)
            || !_cameraEntity.TryGetComponent<TransformComponent>(out var cameraTransform)
            || !_cameraEntity.TryGetComponent<CameraComponent>(out var cameraComponent))
            return;

        HandleMouseLook();

        var playerWorld = playerTransform.GetWorldTransform();
        var playerPos = new Vector3(playerWorld.M41, playerWorld.M42, playerWorld.M43);
        var lookTarget = playerPos + Vector3.UnitY * LookAtHeight;

        var horizontal = FollowDistance * MathF.Cos(_pitch);
        var offset = new Vector3(
            horizontal * MathF.Sin(_yaw),
            FollowDistance * MathF.Sin(_pitch),
            horizontal * MathF.Cos(_yaw));

        var cameraPos = lookTarget + offset;
        var view = Matrix4x4.CreateLookAt(cameraPos, lookTarget, Vector3.UnitY);
        if (!Matrix4x4.Invert(view, out var cameraWorld))
            return;

        cameraComponent.CameraViewTransform = cameraWorld;
        cameraTransform.Translation = cameraPos;
    }

    private void HandleMouseLook()
    {
        if (!mouse.IsButtonDown(MouseButtons.Right))
            return;
        if (cameraQueries.ScreenToWorld2D(mouse.Position) is null)
            return;

        _yaw -= mouse.Delta.X * MouseSensitivity;
        _pitch += -mouse.Delta.Y * MouseSensitivity;
        _pitch = System.Math.Clamp(_pitch, MinPitch, MaxPitch);
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
