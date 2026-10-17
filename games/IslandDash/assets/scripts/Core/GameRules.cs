using Audio;
using ECS;
using SceneComponents;

namespace IslandDash;

/// <summary>Rules shared by several systems (player movement, spin attack, enemy contact, hazards).</summary>
public static class GameRules
{
    private const float CheckpointSpawnHeight = 0.05f;

    public static void KillPlayer(GameStateComponent state, Entity player, PlayerTuningComponent tuning, IAudio audio)
    {
        if (state.Phase != GamePhase.Playing)
            return;

        var controller = player.GetComponent<PlayerComponent>();
        if (controller.InvulnerableTime > 0f)
            return;

        controller.Velocity = Vector3.Zero;
        controller.SpinTime = 0f;
        state.SetPhase(GamePhase.Dying);
        if (player.TryGetComponent<ProceduralAnimatorComponent>(out var animator))
        {
            animator.Spinning = false;
            animator.Fire(AnimationEvent.Die);
        }
        audio.PlaySfx(Sfx.Death, 0.8f);
    }

    public static void AddCrystals(GameStateComponent state, GameSettingsComponent settings, int amount, IAudio audio)
    {
        state.Crystals += amount;
        state.TotalCrystalsCollected += amount;
        while (state.Crystals >= settings.CrystalsPerExtraLife)
        {
            state.Crystals -= settings.CrystalsPerExtraLife;
            GainLife(state, audio);
        }
    }

    public static void GainLife(GameStateComponent state, IAudio audio)
    {
        state.Lives++;
        state.Flash("1UP");
        audio.PlaySfx(Sfx.ExtraLife, 0.8f);
    }

    public static void Collect(Entity crystal, GameStateComponent state, GameSettingsComponent settings, IAudio audio)
    {
        var collectible = crystal.GetComponent<CollectibleComponent>();
        if (collectible.Collected)
            return;

        collectible.Collected = true;
        if (crystal.TryGetComponent<ProceduralAnimatorComponent>(out var animator))
            animator.Fire(AnimationEvent.Pop);
        audio.PlaySfx(Sfx.Collect, 0.55f);
        AddCrystals(state, settings, 1, audio);
    }

    public static void BreakCrate(Context context, Entity crate, GameStateComponent state, GameSettingsComponent settings, IAudio audio)
    {
        var component = crate.GetComponent<CrateComponent>();
        if (component.Broken)
            return;

        component.Broken = true;
        state.CratesBroken++;
        crate.GetComponent<ColliderComponent>().Enabled = false;
        audio.PlaySfx(Sfx.CrateBreak, 0.9f);

        if (crate.TryGetComponent<ProceduralAnimatorComponent>(out var animator))
        {
            if (component.Reward is { } reward && context.Contains(animator.VisualEntityId))
            {
                Spawner.SetModel(context.GetById(animator.VisualEntityId), reward);
                animator.RestScale = reward.Scale;
                animator.RestOffset = reward.Pivot * reward.Scale;
            }
            animator.Fire(AnimationEvent.Pop);
        }

        switch (component.Kind)
        {
            case CrateKind.Normal:
                AddCrystals(state, settings, component.Crystals, audio);
                break;
            case CrateKind.ExtraLife:
                GainLife(state, audio);
                break;
            case CrateKind.Checkpoint:
                var top = crate.GetComponent<TransformComponent>().Translation;
                state.Checkpoint = new Vector3(top.X, top.Y + CheckpointSpawnHeight, top.Z);
                state.Flash("CHECKPOINT");
                audio.PlaySfx(Sfx.Checkpoint, 0.8f);
                break;
        }
    }

    public static void DefeatEnemy(Entity enemy, bool stomped, Vector3 knockDirection, IAudio audio)
    {
        var component = enemy.GetComponent<EnemyComponent>();
        if (!component.Alive)
            return;

        component.Alive = false;
        component.DefeatTime = 0f;
        component.KnockVelocity = stomped ? Vector3.Zero : knockDirection * 9f + new Vector3(0f, 7f, 0f);
        enemy.GetComponent<ColliderComponent>().Enabled = false;
        if (enemy.TryGetComponent<ProceduralAnimatorComponent>(out var animator))
            animator.Fire(stomped ? AnimationEvent.Squash : AnimationEvent.Die);
        audio.PlaySfx(Sfx.Stomp, 0.9f);
    }
}
