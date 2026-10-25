using System.Numerics;
using ECS;
using ECS.Systems;
using SceneComponents;
using Scripting;

namespace Hounds.Marches;

[Register(typeof(IGameSystem))]
public sealed class AISystem(Context context, IPhysicsQueries physics) : IGameSystem
{
    public int Priority => 88;

    public void OnInit() { }

    public void OnShutdown() { }

    public void OnUpdate(TimeSpan deltaTime)
    {
        var game = March.Game(context);
        var player = March.Player(context);
        if (game == null || player == null || game.Hitstop > 0f)
            return;
        if (!player.TryGetComponent<TransformComponent>(out var playerTransform) ||
            !player.TryGetComponent<CombatComponent>(out var playerCombat))
            return;

        var dt = March.Dt(deltaTime);
        var playerAlive = playerCombat.Action != "dead";
        foreach (var (entity, brain) in context.View<AIBrainComponent>())
        {
            if (!entity.TryGetComponent<ActorComponent>(out var actor) ||
                !entity.TryGetComponent<TransformComponent>(out var transform) ||
                !entity.TryGetComponent<CombatComponent>(out var combat) ||
                !entity.TryGetComponent<HealthComponent>(out var health))
                continue;
            if (health.Current <= 0f || combat.Action == "dead")
                continue;

            var x = transform.Translation.X + actor.KnockVx * dt;
            var y = transform.Translation.Y;
            actor.KnockVx *= MathF.Exp(-7f * dt);
            var ground = physics.Raycast(new Vector2(x, y), new Vector2(0, -1), actor.HalfH + 0.25f, entity);
            if (ground is { } hit)
                y = hit.Point.Y + actor.HalfH;
            else
                y -= 16f * dt;

            if (y < game.KillY)
            {
                x = actor.SpawnX;
                y = actor.SpawnY + actor.HalfH;
            }

            var dx = playerTransform.Translation.X - x;
            var adx = MathF.Abs(dx);
            var ady = MathF.Abs(playerTransform.Translation.Y - y);
            if (combat.Action is "stagger")
            {
                transform.Translation = new Vector3(x, y, 0f);
                continue;
            }

            var attacking = combat.Action is not "none";
            if (attacking)
            {
                var atk = SliceRules.For(actor.Kind, combat.Action, combat.Phase2);
                if (combat.Action == "leap" && combat.Frame >= atk.Windup && combat.Frame < atk.Windup + atk.Active)
                {
                    actor.Facing = dx >= 0 ? 1 : -1;
                    x += actor.Facing * 8.2f * dt;
                }
            }
            else if (playerAlive && adx < brain.Aggro && ady < 2.6f && brain.State != "return")
            {
                brain.State = "chase";
            }
            else if (brain.State == "chase" && adx > brain.Leash)
            {
                brain.State = "return";
            }

            if (!attacking && brain.State == "chase" && playerAlive)
            {
                actor.Facing = dx >= 0 ? 1 : -1;
                var reach = SliceRules.For(actor.Kind, NextAttack(actor, combat), combat.Phase2).Reach;
                if (adx <= reach + 0.25f && ground != null && combat.Cooldown <= 0f)
                {
                    combat.Action = NextAttack(actor, combat);
                    combat.Pattern++;
                    combat.Frame = 0;
                    combat.HitDone = false;
                }
                else
                {
                    var speed = actor.Kind == "halberdier" ? 2.4f : actor.Kind == "boss" ? 2.9f : 3.4f;
                    x += actor.Facing * speed * dt;
                    if (!Footing(entity, x, y, actor))
                        x = transform.Translation.X;
                }
            }
            else if (!attacking)
            {
                var home = actor.SpawnX;
                if (brain.State == "return" && MathF.Abs(x - home) < 0.3f)
                    brain.State = "patrol";
                var speed = actor.Kind == "halberdier" ? 1.25f : 1.7f;
                if (brain.State == "return")
                {
                    actor.Facing = home >= x ? 1 : -1;
                    x += actor.Facing * speed * dt;
                }
                else
                {
                    if (x < brain.Left)
                        brain.Dir = 1;
                    if (x > brain.Right)
                        brain.Dir = -1;
                    actor.Facing = brain.Dir;
                    var next = x + brain.Dir * speed * dt;
                    if (!Footing(entity, next, y, actor))
                        brain.Dir = -brain.Dir;
                    else
                        x = next;
                }
            }

            actor.Grounded = ground != null;
            transform.Translation = new Vector3(x, y, 0f);
        }
    }

    private static string NextAttack(ActorComponent actor, CombatComponent combat)
    {
        if (actor.Kind != "boss")
            return "light";
        var step = combat.Pattern;
        if (!combat.Phase2)
            return (step % 3) switch { 0 => "slash", 1 => "thrust", _ => "leap" };
        return (step % 4) switch { 0 => "slash", 1 => "leap", 2 => "thrust", _ => "slash" };
    }

    private bool Footing(Entity self, float x, float y, ActorComponent actor)
    {
        var ahead = physics.Raycast(
            new Vector2(x + actor.Facing * 0.42f, y - actor.HalfH + 0.15f),
            new Vector2(0, -1),
            0.85f,
            self);
        return ahead != null;
    }
}
