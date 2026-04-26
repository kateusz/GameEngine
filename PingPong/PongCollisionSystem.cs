using System;
using System.Numerics;
using ECS;
using ECS.Systems;
using Engine.Scene.Components;
using Engine.Scene.Systems;

namespace PingPong;

internal sealed class PongCollisionSystem(IContext context) : IGameSystem
{
    private const float PaddleHalfWidth = 0.75f;
    private const float PaddleHalfHeight = 2.0f;
    private const float MaxVerticalInfluence = 0.9f;
    private const float MinHorizontalComponent = 0.35f;

    public int Priority => SystemPriorities.PongCollisionSystem;

    public void OnInit() { }

    public void OnUpdate(TimeSpan deltaTime)
    {
        if (IsGameOver())
            return;

        foreach (var (ballEntity, ball) in context.View<BallComponent>())
        {
            if (!ballEntity.TryGetComponent<TransformComponent>(out var ballTransform))
                continue;

            HandleBoundaryCollision(ballTransform, ball);
            HandlePaddleCollision(ballTransform, ball);
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

    private void HandleBoundaryCollision(TransformComponent ballTransform, BallComponent ball)
    {
        foreach (var (boundaryEntity, boundary) in context.View<BoundaryComponent>())
        {
            if (!boundaryEntity.TryGetComponent<TransformComponent>(out var boundaryTransform))
                continue;

            var ballY = ballTransform.Translation.Y;
            var boundaryY = boundaryTransform.Translation.Y;
            var hitTop = boundary.Position == BoundaryPosition.Top && ballY >= boundaryY;
            var hitBottom = boundary.Position == BoundaryPosition.Bottom && ballY <= boundaryY;
            if (!hitTop && !hitBottom)
                continue;

            var clampedTranslation = ballTransform.Translation;
            clampedTranslation.Y = boundaryY;
            ballTransform.Translation = clampedTranslation;
            ball.Velocity = new Vector2(ball.Velocity.X, -ball.Velocity.Y);
            break;
        }
    }

    private void HandlePaddleCollision(TransformComponent ballTransform, BallComponent ball)
    {
        foreach (var (paddleEntity, _) in context.View<PaddleComponent>())
        {
            if (!paddleEntity.TryGetComponent<TransformComponent>(out var paddleTransform))
                continue;

            var delta = ballTransform.Translation - paddleTransform.Translation;
            var overlapsX = MathF.Abs(delta.X) <= PaddleHalfWidth;
            var overlapsY = MathF.Abs(delta.Y) <= PaddleHalfHeight;
            if (!overlapsX || !overlapsY)
                continue;

            var bounceDirectionX = ball.Velocity.X >= 0.0f ? -1.0f : 1.0f;
            var impactOffset = System.Math.Clamp(delta.Y / PaddleHalfHeight, -1.0f, 1.0f);
            var candidateDirection = new Vector2(bounceDirectionX, impactOffset * MaxVerticalInfluence);
            var normalizedDirection = Vector2.Normalize(candidateDirection);
            if (MathF.Abs(normalizedDirection.X) < MinHorizontalComponent)
            {
                var clampedX = MathF.CopySign(MinHorizontalComponent, normalizedDirection.X);
                var clampedY = MathF.CopySign(MathF.Sqrt(1.0f - (clampedX * clampedX)), normalizedDirection.Y);
                normalizedDirection = new Vector2(clampedX, clampedY);
            }

            var currentSpeed = ball.Velocity.Length();
            var speed = currentSpeed > 0.0f ? currentSpeed : ball.Speed;
            ball.Velocity = normalizedDirection * speed;
            break;
        }
    }
}
