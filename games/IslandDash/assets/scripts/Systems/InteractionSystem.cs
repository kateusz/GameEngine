using Audio;
using ECS;
using ECS.Systems;
using Input;
using SceneComponents;
using Scripting;

namespace IslandDash;

/// <summary>Player overlaps: crystals, spin attack on crates/enemies, enemy contact (stomp or die), hazards, kill plane, goal.</summary>
[Register(typeof(IGameSystem))]
public class InteractionSystem(Context context, IKeyboardInput keyboard, IMouseInput mouse, IAudio audio) : IGameSystem
{
    private const float CrystalCenterHeight = 0.35f;
    /// <summary>A contact counts as a stomp when the player's feet are above this fraction of the enemy's height.</summary>
    private const float StompHeightFraction = 0.5f;

    public int Priority => SystemOrder.Interactions;

    public void OnInit() { }

    public void OnShutdown() { }

    public void OnUpdate(TimeSpan deltaTime)
    {
        if (World.State(context) is not { Phase: GamePhase.Playing } state || World.Player(context) is not { } player)
            return;

        var settings = World.Get<GameSettingsComponent>(context);
        var tuning = World.Get<PlayerTuningComponent>(context);
        var pc = player.GetComponent<PlayerComponent>();
        var box = World.PlayerBox(player);
        var center = box.Center;

        Crystals(box, state, settings);
        if (pc.IsSpinning)
            SpinAttack(center, tuning, state, settings);
        Enemies(player, pc, box, tuning, state);
        Hazards(player, box, tuning, state);
        Goal(box, state);

        if (box.Min.Y < state.KillY)
            GameRules.KillPlayer(state, player, tuning, audio);
    }

    private void Crystals(in Box box, GameStateComponent state, GameSettingsComponent settings)
    {
        var collected = new List<Entity>();
        foreach (var (entity, collectible, transform) in context.View<CollectibleComponent, TransformComponent>())
        {
            if (collectible.Collected)
                continue;
            var center = transform.Translation + new Vector3(0f, CrystalCenterHeight, 0f);
            if (box.DistanceSquaredTo(center) <= collectible.Radius * collectible.Radius)
                collected.Add(entity);
        }

        foreach (var crystal in collected)
            GameRules.Collect(crystal, state, settings, audio);
    }

    private void SpinAttack(Vector3 center, PlayerTuningComponent tuning, GameStateComponent state, GameSettingsComponent settings)
    {
        var radiusSq = tuning.SpinRadius * tuning.SpinRadius;
        var crates = new List<Entity>();
        foreach (var (entity, crate, transform) in context.View<CrateComponent, TransformComponent>())
        {
            if (!crate.Broken && Collision.WorldBox(transform, entity.GetComponent<ColliderComponent>()).DistanceSquaredTo(center) <= radiusSq)
                crates.Add(entity);
        }

        foreach (var crate in crates)
            GameRules.BreakCrate(context, crate, state, settings, audio);

        foreach (var (entity, enemy, transform) in context.View<EnemyComponent, TransformComponent>())
        {
            if (!enemy.Alive || Collision.WorldBox(transform, entity.GetComponent<ColliderComponent>()).DistanceSquaredTo(center) > radiusSq)
                continue;
            var away = transform.Translation - center;
            away.Y = 0f;
            GameRules.DefeatEnemy(entity, stomped: false, away.LengthSquared() > 1e-6f ? Vector3.Normalize(away) : Vector3.UnitZ, audio);
        }
    }

    private void Enemies(Entity player, PlayerComponent pc, in Box box, PlayerTuningComponent tuning, GameStateComponent state)
    {
        foreach (var (entity, enemy, transform) in context.View<EnemyComponent, TransformComponent>())
        {
            var collider = entity.GetComponent<ColliderComponent>();
            if (!enemy.Alive || !collider.Enabled)
                continue;

            var enemyBox = Collision.WorldBox(transform, collider);
            if (!box.Overlaps(enemyBox))
                continue;

            var stompLine = enemyBox.Min.Y + (enemyBox.Max.Y - enemyBox.Min.Y) * StompHeightFraction;
            if (pc.Velocity.Y < 0f && box.Min.Y > stompLine)
            {
                GameRules.DefeatEnemy(entity, stomped: true, Vector3.Zero, audio);
                pc.Velocity = pc.Velocity with { Y = PlayerSystem.Bounce(pc, tuning, GameInput.Read(keyboard, mouse)) };
                pc.Grounded = false;
            }
            else if (!pc.IsSpinning)
            {
                GameRules.KillPlayer(state, player, tuning, audio);
                return;
            }
        }
    }

    private void Hazards(Entity player, in Box box, PlayerTuningComponent tuning, GameStateComponent state)
    {
        foreach (var (entity, hazard, transform) in context.View<HazardComponent, TransformComponent>())
        {
            if (!hazard.Active || !entity.TryGetComponent<ColliderComponent>(out var collider) || !collider.Enabled)
                continue;
            if (!box.Overlaps(Collision.WorldBox(transform, collider)))
                continue;

            GameRules.KillPlayer(state, player, tuning, audio);
            return;
        }
    }

    private void Goal(in Box box, GameStateComponent state)
    {
        foreach (var (entity, goal, transform) in context.View<GoalComponent, TransformComponent>())
        {
            if (goal.Reached || !box.Overlaps(Collision.WorldBox(transform, entity.GetComponent<ColliderComponent>())))
                continue;

            goal.Reached = true;
            state.SetPhase(GamePhase.LevelComplete);
            audio.PlaySfx(Sfx.LevelComplete, 0.9f);
            return;
        }
    }
}
