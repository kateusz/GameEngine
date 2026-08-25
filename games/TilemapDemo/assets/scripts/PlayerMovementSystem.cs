using System.Numerics;
using ECS;
using ECS.Systems;
using Input;
using SceneComponents.Physics;
using Scripting;

namespace tilemaps;

/// <summary>
/// A/D walk for the Player. Horizontal velocity is driven by input; Y is left to gravity
/// so the Dynamic body stands on Static ground boxes.
/// </summary>
[Register(typeof(IGameSystem))]
public class PlayerMovementSystem(IContext context, IKeyboardInput keyboard) : IGameSystem
{
    private const float MoveSpeed = 5f;

    public int Priority => 114;

    public void OnInit() { }

    public void OnShutdown() { }

    public void OnUpdate(TimeSpan deltaTime)
    {
        var player = FindPlayer();
        if (player is null || !player.TryGetComponent<RigidBody2DComponent>(out var body))
            return;

        var vx = 0f;
        if (keyboard.IsKeyDown(KeyCodes.A)) vx -= MoveSpeed;
        if (keyboard.IsKeyDown(KeyCodes.D)) vx += MoveSpeed;
        body.Velocity = new Vector2(vx, body.Velocity.Y);
    }

    private Entity? FindPlayer()
    {
        foreach (var entity in context.Entities)
        {
            if (entity.Name == "Player")
                return entity;
        }

        return null;
    }
}
