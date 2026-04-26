using System;
using ECS;
using ECS.Systems;
using Engine.Scene.Components;
using Engine.Scene.Systems;

namespace PingPong;

internal sealed class BallMovementSystem(IContext context) : IGameSystem
{
    public int Priority => SystemPriorities.PongBallMovementSystem;

    public void OnInit() { }

    public void OnUpdate(TimeSpan deltaTime)
    {
        if (IsGameOver())
            return;

        var deltaSeconds = (float)deltaTime.TotalSeconds;
        foreach (var (entity, ball) in context.View<BallComponent>())
        {
            if (!entity.TryGetComponent<TransformComponent>(out var transform))
                continue;

            if (ball.Velocity.LengthSquared() <= float.Epsilon)
                ball.Velocity = new System.Numerics.Vector2(ball.Speed, 0.0f);

            var translation = transform.Translation;
            translation.X += ball.Velocity.X * deltaSeconds;
            translation.Y += ball.Velocity.Y * deltaSeconds;
            transform.Translation = translation;
        }
    }

    public void OnShutdown() { }

    private bool IsGameOver()
    {
        foreach (var (_, score) in context.View<ScoreComponent>())
        {
            if (score.IsGameOver)
                return true;
        }

        return false;
    }
}
