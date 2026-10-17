using Audio;
using ECS;
using ECS.Systems;
using Input;
using SceneComponents;
using Scripting;

namespace IslandDash;

/// <summary>Session phases: title → playing ⇄ paused, dying → respawn at checkpoint or game over, level complete, restart.</summary>
[Register(typeof(IGameSystem))]
public class GameFlowSystem(Context context, IKeyboardInput keyboard, IMouseInput mouse, IAudio audio) : IGameSystem
{
    /// <summary>End screens ignore input briefly so a held jump/confirm doesn't skip them.</summary>
    private const float EndScreenInputDelay = 1f;

    public int Priority => SystemOrder.Flow;

    public void OnInit() { }

    public void OnShutdown() { }

    public void OnUpdate(TimeSpan deltaTime)
    {
        if (World.State(context) is not { } state || state.RebuildRequested)
            return;

        var dt = SystemOrder.ClampDelta(deltaTime);
        var input = GameInput.Read(keyboard, mouse);
        state.PhaseTime += dt;
        if (state.Phase != GamePhase.Paused)
            state.MessageTime = MathF.Max(0f, state.MessageTime - dt);

        switch (state.Phase)
        {
            case GamePhase.Title when input.ConfirmPressed || input.JumpPressed:
                state.SetPhase(GamePhase.Playing);
                state.Flash("GO!", 1.2f);
                audio.PlaySfx(Sfx.Menu);
                break;
            case GamePhase.Playing:
                state.LevelTime += dt;
                if (input.PausePressed)
                {
                    state.SetPhase(GamePhase.Paused);
                    audio.PlaySfx(Sfx.Menu);
                }
                break;
            case GamePhase.Paused when input.PausePressed || input.ConfirmPressed:
                state.SetPhase(GamePhase.Playing);
                audio.PlaySfx(Sfx.Menu);
                break;
            case GamePhase.Dying when state.PhaseTime >= World.Get<PlayerTuningComponent>(context).DeathDuration:
                LoseLife(state);
                break;
            case GamePhase.GameOver or GamePhase.LevelComplete
                when state.PhaseTime > EndScreenInputDelay && (input.ConfirmPressed || input.JumpPressed):
                state.RebuildRequested = true;
                state.SetPhase(GamePhase.Playing);
                state.Flash("GO!", 1.2f);
                audio.PlaySfx(Sfx.Menu);
                break;
        }
    }

    private void LoseLife(GameStateComponent state)
    {
        state.Lives--;
        if (state.Lives <= 0)
        {
            state.Lives = 0;
            state.SetPhase(GamePhase.GameOver);
            audio.PlaySfx(Sfx.GameOver, 0.8f);
            return;
        }

        Respawn(state);
    }

    private void Respawn(GameStateComponent state)
    {
        if (World.Player(context) is not { } player)
            return;

        var tuning = World.Get<PlayerTuningComponent>(context);
        player.GetComponent<TransformComponent>().Translation = state.Checkpoint;
        var pc = player.GetComponent<PlayerComponent>();
        pc.Velocity = Vector3.Zero;
        pc.Grounded = false;
        pc.GroundEntityId = -1;
        pc.SpinTime = 0f;
        pc.JumpConsumed = false;
        pc.AirJumpsUsed = 0;
        pc.TimeSinceJumpPressed = float.MaxValue;
        pc.InvulnerableTime = tuning.RespawnInvulnerability;

        var animator = player.GetComponent<ProceduralAnimatorComponent>();
        animator.FacingYaw = MathF.PI + state.CameraYaw;
        animator.Fire(AnimationEvent.Respawn);

        EnemySystem.ResetAll(context);
        state.CameraSnap = true;
        state.SetPhase(GamePhase.Playing);
    }
}
