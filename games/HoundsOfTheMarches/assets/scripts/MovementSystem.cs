using System.Numerics;
using ECS;
using ECS.Systems;
using Input;
using SceneComponents;
using SceneComponents.Physics;
using Scripting;

namespace Hounds.Marches;

[Register(typeof(IGameSystem))]
public sealed class MovementSystem(Context context, IKeyboardInput keyboard, IPhysicsQueries physics) : IGameSystem
{
    public int Priority => 92;

    public void OnInit() { }

    public void OnShutdown() { }

    public void OnUpdate(TimeSpan deltaTime)
    {
        var game = March.Game(context);
        var player = March.Player(context);
        if (game == null || player == null)
            return;
        if (!player.TryGetComponent<ActorComponent>(out var actor) ||
            !player.TryGetComponent<TransformComponent>(out var transform) ||
            !player.TryGetComponent<RigidBody2DComponent>(out var body) ||
            !player.TryGetComponent<CombatComponent>(out var combat))
            return;

        var dt = March.Dt(deltaTime);
        var velocity = body.Velocity;
        if (game.Hitstop > 0f || combat.Action is "dead" or "stagger")
        {
            body.Velocity = Vector2.Zero;
            return;
        }

        var left = keyboard.IsKeyDown(KeyCodes.A) || keyboard.IsKeyDown(KeyCodes.Left);
        var right = keyboard.IsKeyDown(KeyCodes.D) || keyboard.IsKeyDown(KeyCodes.Right);
        var move = (right ? 1 : 0) - (left ? 1 : 0);
        if (combat.Action is "none" or "block" && move != 0)
            actor.Facing = move;

        actor.InPuddle = OverZone(transform, "puddle");
        var wantLadder = OverZone(transform, "ladder")
                         && combat.Action is "none" or "block"
                         && (keyboard.IsKeyDown(KeyCodes.W) || keyboard.IsKeyDown(KeyCodes.S) || keyboard.IsKeyDown(KeyCodes.Up) || keyboard.IsKeyDown(KeyCodes.Down) || actor.OnLadder);
        if (keyboard.WasKeyPressed(KeyCodes.Space))
            wantLadder = false;
        actor.OnLadder = wantLadder;

        var speed = SliceRules.RunSpeed;
        if (actor.InPuddle)
            speed *= 0.62f;
        if (combat.Action == "block")
            speed *= 0.38f;
        if (combat.Action is "light" or "heavy")
            speed = combat.Frame < 8 ? speed * 0.2f : 0f;

        if (actor.OnLadder)
        {
            if (body.GravityScale != 0f)
                body.GravityScale = 0f;
            var climb = 0f;
            if (keyboard.IsKeyDown(KeyCodes.W) || keyboard.IsKeyDown(KeyCodes.Up))
                climb = 1f;
            if (keyboard.IsKeyDown(KeyCodes.S) || keyboard.IsKeyDown(KeyCodes.Down))
                climb = -1f;
            body.Velocity = new Vector2(move * speed * 0.45f, climb * 3.3f);
            actor.Grounded = false;
            return;
        }

        if (body.GravityScale != 1f)
            body.GravityScale = 1f;

        var probe = physics.Raycast(
            new Vector2(transform.Translation.X, transform.Translation.Y),
            new Vector2(0f, -1f),
            actor.HalfH + 0.14f,
            player);
        actor.Grounded = probe != null && combat.Action != "dodge";
        actor.Coyote = actor.Grounded ? 0.1f : MathF.Max(0f, actor.Coyote - dt);
        if (keyboard.WasKeyPressed(KeyCodes.Space))
            actor.JumpBuffer = 0.12f;
        else
            actor.JumpBuffer = MathF.Max(0f, actor.JumpBuffer - dt);

        if (combat.Action == "dodge")
        {
            velocity.X = actor.Facing * SliceRules.DodgeSpeed;
            if (combat.Frame <= 1 && actor.Coyote > 0f)
                velocity.Y = 2.4f;
        }
        else
        {
            velocity.X = move * speed;
        }

        var canJump = combat.Action is "none" or "block";
        if (canJump && actor.JumpBuffer > 0f && (actor.Grounded || actor.Coyote > 0f))
        {
            velocity.Y = SliceRules.JumpSpeed;
            actor.JumpBuffer = 0f;
            actor.Coyote = 0f;
            actor.Grounded = false;
        }

        body.Velocity = velocity;

        if (transform.Translation.Y < game.KillY)
        {
            var health = player.GetComponent<HealthComponent>();
            health.Current = 0f;
            combat.Action = "dead";
        }
    }

    private bool OverZone(TransformComponent transform, string kind)
    {
        foreach (var (entity, zone) in context.View<ZoneComponent>())
        {
            if (zone.Kind != kind || !entity.TryGetComponent<TransformComponent>(out var zoneTransform))
                continue;
            if (MathF.Abs(transform.Translation.X - zoneTransform.Translation.X) <= zone.W * 0.5f &&
                MathF.Abs(transform.Translation.Y - zoneTransform.Translation.Y) <= zone.H * 0.5f)
                return true;
        }

        return false;
    }
}
