using System.Numerics;
using ECS;
using ECS.Systems;
using SceneComponents;
using SceneComponents.Physics;
using Scripting;

namespace PingPong;

[Register(typeof(IGameSystem))]
internal sealed class BallMovementSystem(IContext context) : IGameSystem
{
    public int Priority => 103;

    public void OnInit()
    {
        var scoreEntity = context.GetByName(PongEntityNames.Score);
        if (scoreEntity is not null && !scoreEntity.HasComponent<ScoreComponent>())
            scoreEntity.AddComponent(new ScoreComponent { MaxScore = PongConstants.MaxScore });
    }

    public void OnUpdate(TimeSpan deltaTime)
    {
        foreach (var (entity, ball) in context.View<BallComponent>())
        {
            if (!entity.TryGetComponent<RigidBody2DComponent>(out var rigidBody))
                continue;
            if (!entity.TryGetComponent<TransformComponent>(out var transform))
                continue;

            var scoreEntity = context.GetByName(PongEntityNames.Score);
            if (scoreEntity?.TryGetComponent<ScoreComponent>(out var score) != true)
                return;

            // --- Scoring ---
            if (PongScoreHandler.PlayerScored)
            {
                score.PlayerScore = Math.Min(score.PlayerScore + 1, score.MaxScore);
                PongScoreHandler.PlayerScored = false;
            }
            if (PongScoreHandler.AiScored)
            {
                score.AiScore = Math.Min(score.AiScore + 1, score.MaxScore);
                PongScoreHandler.AiScored = false;
            }

            // --- Game over ---
            if (score.PlayerScore >= score.MaxScore || score.AiScore >= score.MaxScore)
            {
                score.IsGameOver = true;
                rigidBody.Velocity = Vector2.Zero;
                return;
            }

            if (score.IsGameOver)
            {
                rigidBody.Velocity = Vector2.Zero;
                return;
            }

            // --- Failsafe: ball escaped without collision (tunneling) ---
            if (MathF.Abs(transform.Translation.X) > PongConstants.GoalX)
            {
                if (transform.Translation.X < 0)
                    PongScoreHandler.AiScored = true;
                else
                    PongScoreHandler.PlayerScored = true;

                transform.Translation = Vector3.Zero;
                ball.ReadyToLaunch = true;
                continue;
            }

            // --- Launch ---
            if (ball.ReadyToLaunch)
            {
                var dir = ball.LaunchDirection > 0 ? 1.0f : -1.0f;
                var randomY = (Random.Shared.NextSingle() * 2.0f - 1.0f) * 0.7f;
                var launchVelocity = Vector2.Normalize(new Vector2(dir, randomY)) * ball.Speed;

                if (MathF.Abs(launchVelocity.Y) < 0.25f)
                    launchVelocity.Y = launchVelocity.Y >= 0 ? 0.25f : -0.25f;

                rigidBody.Velocity = launchVelocity;
                ball.ReadyToLaunch = false;
                continue;
            }

            // --- Speed normalization ---
            var currentVelocity = rigidBody.Velocity;
            if (currentVelocity.LengthSquared() > float.Epsilon)
                rigidBody.Velocity = Vector2.Normalize(currentVelocity) * ball.Speed;
        }
    }

    public void OnShutdown() { }
}
