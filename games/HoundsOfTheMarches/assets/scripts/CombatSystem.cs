using System.Numerics;
using Audio;
using ECS;
using ECS.Systems;
using Input;
using SceneComponents;
using SceneComponents.Physics;
using Scripting;

namespace Hounds.Marches;

[Register(typeof(IGameSystem))]
public sealed class CombatSystem(Context context, IKeyboardInput keyboard, IAudio audio) : IGameSystem
{
    public int Priority => 90;

    public void OnInit() { }

    public void OnShutdown() { }

    public void OnUpdate(TimeSpan deltaTime)
    {
        var game = March.Game(context);
        if (game == null)
            return;

        var dt = March.Dt(deltaTime);
        if (game.Hitstop > 0f)
        {
            game.Hitstop -= dt;
            if (game.Hitstop > 0f)
                return;
        }

        foreach (var (entity, actor) in context.View<ActorComponent>())
        {
            if (!entity.TryGetComponent<CombatComponent>(out var combat) ||
                !entity.TryGetComponent<HealthComponent>(out var health))
                continue;

            if (combat.Invuln > 0f)
                combat.Invuln -= dt;
            if (combat.Cooldown > 0f)
                combat.Cooldown -= dt;
            if (health.PoiseDelay > 0f)
                health.PoiseDelay -= dt;
            else if (health.Poise < health.PoiseMax && combat.Action is not ("dead" or "stagger"))
                health.Poise = MathF.Min(health.PoiseMax, health.Poise + 16f * dt);

            if (entity.TryGetComponent<StaminaComponent>(out var stamina))
            {
                if (stamina.RegenDelay > 0f)
                    stamina.RegenDelay -= dt;
                else if (combat.Action == "none")
                    stamina.Current = MathF.Min(stamina.Max, stamina.Current + 22f * dt);
            }

            if (actor.Kind == "player" && combat.Action == "none" && health.Current > 0f)
                TryStartPlayer(entity, actor, combat);

            Tick(game, entity, actor, combat, health);
        }
    }

    private void TryStartPlayer(Entity entity, ActorComponent actor, CombatComponent combat)
    {
        entity.TryGetComponent<StaminaComponent>(out var stamina);
        var dodgeDir = actor.Facing;
        if (keyboard.IsKeyDown(KeyCodes.A) || keyboard.IsKeyDown(KeyCodes.Left))
            dodgeDir = -1;
        if (keyboard.IsKeyDown(KeyCodes.D) || keyboard.IsKeyDown(KeyCodes.Right))
            dodgeDir = 1;

        if (keyboard.WasKeyPressed(KeyCodes.LeftShift) && Spend(stamina, SliceRules.For("player", "dodge", false).Stamina))
        {
            actor.Facing = dodgeDir;
            Begin(combat, "dodge");
            MarchAudio.Play(audio, "swing");
            return;
        }

        if (keyboard.WasKeyPressed(KeyCodes.K) && Spend(stamina, SliceRules.For("player", "heavy", false).Stamina))
        {
            Begin(combat, "heavy");
            MarchAudio.Play(audio, "swing");
            return;
        }

        if (keyboard.WasKeyPressed(KeyCodes.J) && Spend(stamina, SliceRules.For("player", "light", false).Stamina))
        {
            Begin(combat, "light");
            MarchAudio.Play(audio, "swing");
            return;
        }

        if (keyboard.IsKeyDown(KeyCodes.L))
            Begin(combat, "block");
    }

    private static bool Spend(StaminaComponent? stamina, float cost)
    {
        if (stamina == null || stamina.Current < cost)
            return false;
        stamina.Current -= cost;
        stamina.RegenDelay = 0.45f;
        return true;
    }

    private static void Begin(CombatComponent combat, string action)
    {
        combat.Action = action;
        combat.Frame = 0;
        combat.HitDone = false;
    }

    private void Tick(MarchGameComponent game, Entity entity, ActorComponent actor, CombatComponent combat, HealthComponent health)
    {
        if (health.Current <= 0f)
        {
            combat.Action = "dead";
            return;
        }

        if (combat.Action == "block")
        {
            if (!keyboard.IsKeyDown(KeyCodes.L) || actor.Kind != "player")
                combat.Action = "none";
            else
                combat.Frame++;
            return;
        }

        if (combat.Action == "dodge")
        {
            if (Melee.InDodgeIFrames(combat.Action, combat.Frame))
                combat.Invuln = 0.08f;
            combat.Frame++;
            if (combat.Frame >= SliceRules.DodgeFrames)
                combat.Action = "none";
            return;
        }

        if (combat.Action == "stagger")
        {
            combat.Frame++;
            if (combat.Frame >= SliceRules.StaggerFrames)
                combat.Action = "none";
            return;
        }

        if (combat.Action is "none" or "dead")
            return;

        var atk = SliceRules.For(actor.Kind, combat.Action, combat.Phase2);
        if (!combat.HitDone && combat.Frame >= atk.Windup && combat.Frame < atk.Windup + atk.Active)
            Strike(game, entity, actor, combat, atk);

        combat.Frame++;
        if (combat.Frame >= atk.Windup + atk.Active + atk.Recover)
        {
            combat.Action = "none";
            combat.Cooldown = actor.Kind == "boss" && combat.Phase2 ? 0.45f : 0.85f;
        }
    }

    private void Strike(MarchGameComponent game, Entity attacker, ActorComponent atkActor, CombatComponent atkCombat, SliceRules.Attack atk)
    {
        if (!attacker.TryGetComponent<TransformComponent>(out var origin))
            return;
        Melee.Hitbox(atk, atkActor.Facing, origin.Translation.X, origin.Translation.Y, out var hx, out var hy, out var hw, out var hh);
        var connected = false;
        foreach (var (victim, victimActor) in context.View<ActorComponent>())
        {
            if (victim.Id == attacker.Id || victimActor.Team == atkActor.Team)
                continue;
            if (!victim.TryGetComponent<TransformComponent>(out var vt) ||
                !victim.TryGetComponent<HealthComponent>(out var hp) ||
                !victim.TryGetComponent<CombatComponent>(out var vc))
                continue;
            if (hp.Current <= 0f)
                continue;
            if (!Melee.Aabb(hx, hy, hw, hh, vt.Translation.X, vt.Translation.Y, victimActor.HalfW, victimActor.HalfH))
                continue;
            if (vc.Invuln > 0f || Melee.InDodgeIFrames(vc.Action, vc.Frame))
                continue;

            connected = true;

            if (vc.Action == "block" && victimActor.Kind == "player")
            {
                if (Melee.InParry(vc.Frame))
                {
                    atkCombat.Action = "stagger";
                    atkCombat.Frame = 0;
                    atkCombat.HitDone = true;
                    if (victim.TryGetComponent<StaminaComponent>(out var stamina))
                        stamina.Current = MathF.Min(stamina.Max, stamina.Current + 10f);
                    game.Hitstop = 0.12f;
                    MarchAudio.Play(audio, "parry");
                    return;
                }

                victim.TryGetComponent<StaminaComponent>(out var blockStamina);
                if (blockStamina == null || blockStamina.Current < 16f)
                {
                    vc.Action = "stagger";
                    vc.Frame = 0;
                }
                else
                {
                    blockStamina.Current -= 16f;
                    blockStamina.RegenDelay = 0.35f;
                }

                Knock(victim, victimActor, MathF.Sign(vt.Translation.X - origin.Translation.X), atk.Knock * 0.35f);
                game.Hitstop = 0.05f;
                MarchAudio.Play(audio, "hit");
                continue;
            }

            hp.Current -= atk.Damage;
            hp.Poise -= atk.Poise;
            hp.PoiseDelay = 1.3f;
            victimActor.Flash = 0.09f;
            var dir = MathF.Sign(vt.Translation.X - origin.Translation.X);
            if (dir == 0)
                dir = atkActor.Facing;
            Knock(victim, victimActor, dir, atk.Knock);
            if (hp.Poise <= 0f)
            {
                hp.Poise = hp.PoiseMax;
                vc.Action = "stagger";
                vc.Frame = 0;
            }

            game.Hitstop = atk.Damage >= 24f ? 0.09f : 0.05f;
            MarchAudio.Play(audio, hp.Current <= 0f ? "death" : "hit");
            if (hp.Current <= 0f)
            {
                hp.Current = 0f;
                vc.Action = "dead";
                if (!victimActor.Rewarded && victimActor.Team == 1)
                {
                    victimActor.Rewarded = true;
                    game.Stingers += victimActor.StingerValue;
                }

                if (victimActor.Kind == "boss")
                {
                    game.Won = true;
                    game.BannerKey = "title_victory";
                    game.BannerTimer = 5f;
                }
            }
            else if (victimActor.Kind == "boss" && !vc.Phase2 && hp.Current <= hp.Max * 0.5f)
            {
                vc.Phase2 = true;
                vc.Action = "stagger";
                vc.Frame = 0;
                game.BannerKey = "title_phase";
                game.BannerTimer = 2.6f;
            }
        }

        if (connected)
            atkCombat.HitDone = true;
    }

    private static void Knock(Entity victim, ActorComponent actor, float dir, float force)
    {
        if (dir == 0)
            dir = 1;
        if (victim.TryGetComponent<RigidBody2DComponent>(out var body))
            body.Velocity = new Vector2(dir * force, MathF.Max(body.Velocity.Y, 2.4f));
        else
            actor.KnockVx = dir * force;
    }
}
