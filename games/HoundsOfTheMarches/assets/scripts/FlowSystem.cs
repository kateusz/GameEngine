using System.Numerics;
using Audio;
using ECS;
using ECS.Systems;
using Engine.Scene;
using Input;
using SceneComponents;
using SceneComponents.Physics;
using Scripting;

namespace Hounds.Marches;

[Register(typeof(IGameSystem))]
public sealed class FlowSystem(Context context, IKeyboardInput keyboard, IAudio audio, ISceneContext scenes) : IGameSystem
{
    private bool _deathSent;
    private bool _exitRequested;

    public int Priority => 96;

    public void OnInit() { }

    public void OnShutdown() { }

    public void OnUpdate(TimeSpan deltaTime)
    {
        var game = March.Game(context);
        var player = March.Player(context);
        if (game == null || player == null)
            return;
        if (game.Hitstop > 0f)
            return;
        if (!player.TryGetComponent<TransformComponent>(out var transform) ||
            !player.TryGetComponent<ActorComponent>(out var actor) ||
            !player.TryGetComponent<CombatComponent>(out var combat) ||
            !player.TryGetComponent<HealthComponent>(out var health) ||
            !player.TryGetComponent<RigidBody2DComponent>(out var body))
            return;

        var dt = March.Dt(deltaTime);
        var feetX = transform.Translation.X;
        var feetY = transform.Translation.Y - actor.HalfH;

        if (combat.Action == "dead")
        {
            if (!_deathSent)
            {
                _deathSent = true;
                game.DeathTimer = 1.25f;
                if (game.Stingers > 0)
                {
                    game.BloodAmount = game.Stingers;
                    game.BloodX = feetX;
                    game.BloodY = feetY;
                    game.BloodActive = true;
                    game.Stingers = 0;
                }

                game.BannerKey = "title_dead";
                game.BannerTimer = 1.6f;
                MarchAudio.Play(audio, "death");
            }

            game.DeathTimer -= dt;
            if (game.DeathTimer <= 0f)
                Respawn(game, player, transform, actor, combat, health, body);
        }
        else
        {
            _deathSent = false;
        }

        if (game.HelpTimer > 0f)
            game.HelpTimer -= dt;
        if (game.BannerTimer > 0f)
            game.BannerTimer -= dt;
        game.Fade = MathF.Max(0f, game.Fade - dt * 1.5f);

        if (!_exitRequested && game.NextScene.Length > 0 && feetX >= game.ExitX && combat.Action != "dead")
        {
            _exitRequested = true;
            scenes.RequestLoad(game.NextScene);
            return;
        }

        game.ProximityKey = "";
        foreach (var (entity, prop) in context.View<PropComponent>())
        {
            if (!entity.TryGetComponent<TransformComponent>(out var propTransform))
                continue;
            if (prop.Kind == "blood")
                propTransform.Translation = new Vector3(game.BloodX, game.BloodY + 0.2f, 0f);
            var dist = Vector2.Distance(new Vector2(feetX, feetY), new Vector2(propTransform.Translation.X, propTransform.Translation.Y));
            if (prop.Kind == "pickup" && !prop.Taken && dist < 0.85f)
            {
                prop.Taken = true;
                game.Stingers += prop.Amount;
                March.Show(entity, false);
                MarchAudio.Play(audio, "checkpoint");
            }
            else if (prop.Kind == "blood")
            {
                March.Show(entity, game.BloodActive);
                if (game.BloodActive && combat.Action != "dead" && dist < 0.9f)
                {
                    game.Stingers += game.BloodAmount;
                    game.BloodActive = false;
                    game.BloodAmount = 0;
                    game.BannerKey = "title_blood";
                    game.BannerTimer = 1.6f;
                    MarchAudio.Play(audio, "checkpoint");
                    March.Show(entity, false);
                }
            }
            else if (prop.Kind == "inscription" && dist < 1.5f)
            {
                game.ProximityKey = prop.ImageKey;
            }
        }

        if (keyboard.WasKeyPressed(KeyCodes.E) && combat.Action != "dead")
        {
            foreach (var (entity, checkpoint) in context.View<CheckpointComponent>())
            {
                if (!entity.TryGetComponent<TransformComponent>(out var shrine))
                    continue;
                if (Vector2.Distance(new Vector2(feetX, transform.Translation.Y), new Vector2(shrine.Translation.X, shrine.Translation.Y)) > 1.45f)
                    continue;
                game.RespawnX = shrine.Translation.X;
                game.RespawnY = shrine.Translation.Y - 0.85f;
                health.Current = health.Max;
                health.Poise = health.PoiseMax;
                if (player.TryGetComponent<StaminaComponent>(out var stamina))
                {
                    stamina.Current = stamina.Max;
                    stamina.RegenDelay = 0f;
                }

                ResetEnemies();
                game.BannerKey = "title_rest";
                game.BannerTimer = 1.4f;
                MarchAudio.Play(audio, "checkpoint");
                break;
            }
        }
        else
        {
            foreach (var (entity, _) in context.View<CheckpointComponent>())
            {
                if (!entity.TryGetComponent<TransformComponent>(out var shrine))
                    continue;
                if (Vector2.Distance(new Vector2(feetX, transform.Translation.Y), new Vector2(shrine.Translation.X, shrine.Translation.Y)) < 1.45f
                    && game.BannerTimer <= 0f)
                    game.ProximityKey = game.ProximityKey.Length == 0 ? "title_rest" : game.ProximityKey;
            }
        }
    }

    private void Respawn(MarchGameComponent game, Entity player, TransformComponent transform, ActorComponent actor,
        CombatComponent combat, HealthComponent health, RigidBody2DComponent body)
    {
        transform.Translation = new Vector3(game.RespawnX, game.RespawnY + actor.HalfH, 0f);
        body.Velocity = Vector2.Zero;
        body.IsBullet = !body.IsBullet;
        health.Current = health.Max;
        health.Poise = health.PoiseMax;
        if (player.TryGetComponent<StaminaComponent>(out var stamina))
            stamina.Current = stamina.Max * 0.65f;
        combat.Action = "none";
        combat.Frame = 0;
        combat.Invuln = 1.1f;
        game.DeathTimer = -1f;
        _deathSent = false;
        ResetEnemies();
    }

    private void ResetEnemies()
    {
        foreach (var (entity, actor) in context.View<ActorComponent>())
        {
            if (actor.Team == 0)
                continue;
            if (!entity.TryGetComponent<TransformComponent>(out var transform) ||
                !entity.TryGetComponent<HealthComponent>(out var health) ||
                !entity.TryGetComponent<CombatComponent>(out var combat))
                continue;
            transform.Translation = new Vector3(actor.SpawnX, actor.SpawnY + actor.HalfH, 0f);
            health.Current = health.Max;
            health.Poise = health.PoiseMax;
            combat.Action = "none";
            combat.Frame = 0;
            combat.Phase2 = false;
            combat.HitDone = false;
            combat.Cooldown = 0.4f;
            actor.Rewarded = false;
            actor.KnockVx = 0f;
            actor.Flash = 0f;
            March.Show(entity, true);
        }
    }
}
