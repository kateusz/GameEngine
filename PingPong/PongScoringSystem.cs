using System;
using System.Linq;
using System.Numerics;
using ECS;
using ECS.Systems;
using Engine.Scene.Components;
using Engine.Scene.Systems;

namespace PingPong;

internal sealed class PongScoringSystem(IContext context) : IGameSystem
{
    private const float GoalX = 11.0f;

    public int Priority => SystemPriorities.PongScoringSystem;

    public void OnInit() { }

    public void OnUpdate(TimeSpan deltaTime)
    {
        var scoreEntity = context.View<ScoreComponent>().FirstOrDefault();
        if (scoreEntity == default)
            return;

        var score = scoreEntity.Component;
        if (score.IsGameOver)
            return;

        foreach (var (ballEntity, ball) in context.View<BallComponent>())
        {
            if (!ballEntity.TryGetComponent<TransformComponent>(out var ballTransform))
                continue;

            var scoredThisTick = false;
            if (ballTransform.Translation.X < -GoalX)
            {
                score.AiScore++;
                ResetBall(ball, ballTransform, serveRight: true);
                scoredThisTick = true;
            }
            else if (ballTransform.Translation.X > GoalX)
            {
                score.PlayerScore++;
                ResetBall(ball, ballTransform, serveRight: false);
                scoredThisTick = true;
            }

            if (!scoredThisTick)
                continue;

            if (score.PlayerScore >= score.MaxScore || score.AiScore >= score.MaxScore)
                score.IsGameOver = true;

            return;
        }
    }

    public void OnShutdown() { }

    private static void ResetBall(BallComponent ball, TransformComponent ballTransform, bool serveRight)
    {
        var direction = serveRight ? 1.0f : -1.0f;
        ballTransform.Translation = Vector3.Zero;
        ball.Velocity = new Vector2(direction * ball.Speed, 0.0f);
    }
}
