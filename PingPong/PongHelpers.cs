using System.Numerics;
using ECS;
using SceneComponents;
using SceneComponents.Physics;

namespace PingPong;

internal static class PongHelpers
{
    public static Entity RequireEntity(IContext context, string name)
    {
        var entity = context.GetByName(name);
        if (entity is null)
            throw new InvalidOperationException($"Ping Pong scene is missing required entity '{name}'.");
        return entity;
    }

    public static bool IsGameOver(IContext context)
    {
        foreach (var (_, score) in context.View<ScoreComponent>())
        {
            if (score.IsGameOver)
                return true;
        }

        return false;
    }

    public static (float? TopBoundary, float? BottomBoundary) GetBoundaryLimits(IContext context)
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

    public static float GetPaddleHalfHeight(Entity entity)
    {
        if (entity.TryGetComponent<TransformComponent>(out var transform) &&
            entity.TryGetComponent<BoxCollider2DComponent>(out var collider))
        {
            return collider.Size.Y * transform.Scale.Y;
        }

        if (entity.TryGetComponent<TransformComponent>(out transform))
            return transform.Scale.Y * 0.5f;

        return 0.5f;
    }

    public static float ClampPaddleY(
        float targetY,
        Entity paddleEntity,
        float? topBoundary,
        float? bottomBoundary)
    {
        if (!topBoundary.HasValue || !bottomBoundary.HasValue)
            return targetY;

        var halfHeight = GetPaddleHalfHeight(paddleEntity);
        var minY = bottomBoundary.Value + halfHeight;
        var maxY = topBoundary.Value - halfHeight;
        return System.Math.Clamp(targetY, minY, maxY);
    }

    public static float ResolveBallRadius(Entity entity)
    {
        if (!entity.TryGetComponent<TransformComponent>(out var transform))
            return 0.25f;

        if (entity.TryGetComponent<BoxCollider2DComponent>(out var collider))
        {
            return MathF.Min(
                collider.Size.X * transform.Scale.X,
                collider.Size.Y * transform.Scale.Y);
        }

        return MathF.Min(transform.Scale.X, transform.Scale.Y) * 0.5f;
    }

    public static Vector2 GetHalfExtents(Entity entity, TransformComponent transform)
    {
        if (entity.TryGetComponent<BoxCollider2DComponent>(out var collider))
        {
            return new Vector2(
                collider.Size.X * transform.Scale.X,
                collider.Size.Y * transform.Scale.Y);
        }

        return new Vector2(transform.Scale.X * 0.5f, transform.Scale.Y * 0.5f);
    }
}
