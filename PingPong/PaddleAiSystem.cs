using ECS;
using ECS.Systems;
using SceneComponents;
using Scripting;

namespace PingPong;

[Register(typeof(IGameSystem))]
internal sealed class PaddleAiSystem(IContext context) : IGameSystem
{
    private const float VerticalDeadzone = 0.1f;

    public int Priority => 102;

    public void OnInit()
    {
        var score = context.GetByName("AI Paddle");
        score.AddComponent<PaddleComponent>();
    }
    
    public void OnUpdate(TimeSpan deltaTime)
    {
        if (IsGameOver())
            return;

        var targetY = GetBallY();
        if (!targetY.HasValue)
            return;

        var (topBoundary, bottomBoundary) = GetBoundaryLimits();
        var deltaSeconds = (float)deltaTime.TotalSeconds;

        var entity = context.GetByName("AI Paddle");
        var transform = entity.GetComponent<TransformComponent>();
        var paddle = entity.GetComponent<PaddleComponent>();

        var currentY = transform.Translation.Y;
        var deltaY = targetY.Value - currentY;
        if (MathF.Abs(deltaY) <= VerticalDeadzone)
            return;

        var direction = MathF.Sign(deltaY);
        var translation = transform.Translation;
        translation.Y += direction * paddle.MoveSpeed * deltaSeconds;
        translation.Y = ClampPaddleY(translation.Y, transform.Scale.Y, topBoundary, bottomBoundary);
        transform.Translation = translation;
    }

    public void OnShutdown()
    {
    }

    private bool IsGameOver()
    {
        foreach (var (_, score) in context.View<ScoreComponent>())
        {
            if (score.IsGameOver)
                return true;
        }

        return false;
    }

    private float? GetBallY()
    {
        foreach (var (entity, _) in context.View<BallComponent>())
        {
            if (entity.TryGetComponent<TransformComponent>(out var transform))
                return transform.Translation.Y;
        }

        return null;
    }

    private (float? TopBoundary, float? BottomBoundary) GetBoundaryLimits()
    {
        float? topBoundary = null;
        float? bottomBoundary = null;

        foreach (var (entity, boundary) in context.View<BoundaryComponent>())
        {
            if (!entity.TryGetComponent<TransformComponent>(out var transform))
                continue;

            if (boundary.Position == BoundaryPosition.Top)
                topBoundary = transform.Translation.Y;
            else if (boundary.Position == BoundaryPosition.Bottom)
                bottomBoundary = transform.Translation.Y;
        }

        return (topBoundary, bottomBoundary);
    }

    private static float ClampPaddleY(float targetY, float paddleScaleY, float? topBoundary, float? bottomBoundary)
    {
        if (!topBoundary.HasValue || !bottomBoundary.HasValue)
            return targetY;

        var halfHeight = paddleScaleY * 0.5f;
        var minY = bottomBoundary.Value + halfHeight;
        var maxY = topBoundary.Value - halfHeight;
        return System.Math.Clamp(targetY, minY, maxY);
    }
}