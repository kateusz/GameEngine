using ECS;
using SceneComponents;

namespace IslandDash;

/// <summary>
/// Minimal 3D collision layer over <see cref="ColliderComponent"/> boxes.
/// Movement is resolved one axis at a time with an exact swept test per axis, so a fast body
/// can never pass through a box regardless of frame time (no tunneling).
/// </summary>
// ponytail: every query scans all colliders (O(n), ~300 boxes in level 1). Upgrade path: uniform grid
// or sort-and-sweep broadphase keyed by the X/Z cell once levels get much bigger.
public static class Collision
{
    /// <summary>Contact tolerance: touching within this distance is treated as resting contact, not overlap.</summary>
    public const float Skin = 0.002f;

    public static Box WorldBox(TransformComponent transform, ColliderComponent collider) =>
        Box.FromCenter(transform.Translation + collider.Offset, collider.HalfExtents);

    /// <summary>
    /// Moves <paramref name="box"/> along <paramref name="axis"/> by up to <paramref name="delta"/> and returns
    /// the distance actually allowed before the first solid collider.
    /// </summary>
    public static float SweepAxis(Context context, in Box box, int axis, float delta, int ignoreEntityId, out Entity? hit)
    {
        hit = null;
        if (delta == 0f)
            return 0f;

        foreach (var (entity, collider, transform) in context.View<ColliderComponent, TransformComponent>())
        {
            if (!collider.Solid || !collider.Enabled || entity.Id == ignoreEntityId)
                continue;

            var other = WorldBox(transform, collider);
            if (!OverlapsOnOtherAxes(box, other, axis))
                continue;

            if (delta > 0f)
            {
                var gap = GameMath.Get(other.Min, axis) - GameMath.Get(box.Max, axis);
                if (gap >= -Skin && gap < delta)
                {
                    delta = MathF.Max(gap, 0f);
                    hit = entity;
                }
            }
            else
            {
                var gap = GameMath.Get(other.Max, axis) - GameMath.Get(box.Min, axis);
                if (gap <= Skin && gap > delta)
                {
                    delta = MathF.Min(gap, 0f);
                    hit = entity;
                }
            }
        }

        return delta;
    }

    /// <summary>Pushes <paramref name="box"/> out of any solid it overlaps along the shallowest axis. Returns the correction.</summary>
    public static Vector3 Depenetrate(Context context, Box box, int ignoreEntityId)
    {
        var total = Vector3.Zero;
        foreach (var (entity, collider, transform) in context.View<ColliderComponent, TransformComponent>())
        {
            if (!collider.Solid || !collider.Enabled || entity.Id == ignoreEntityId)
                continue;

            var other = WorldBox(transform, collider);
            if (!box.Overlaps(other, Skin))
                continue;

            var push = ShallowestPush(box, other);
            box = box.Translate(push);
            total += push;
        }

        return total;
    }

    /// <summary>Distance along the ray to the first collider that blocks the camera, or <paramref name="maxDistance"/>.</summary>
    public static float Raycast(Context context, Vector3 origin, Vector3 direction, float maxDistance)
    {
        var closest = maxDistance;
        foreach (var (_, collider, transform) in context.View<ColliderComponent, TransformComponent>())
        {
            if (!collider.Solid || !collider.Enabled || !collider.BlocksCamera)
                continue;

            if (RayBox(origin, direction, WorldBox(transform, collider), out var t) && t < closest)
                closest = t;
        }

        return closest;
    }

    /// <summary>Slab test. Rays starting inside a box report no hit, so the camera never snaps onto the ground the player stands in.</summary>
    public static bool RayBox(Vector3 origin, Vector3 direction, in Box box, out float distance)
    {
        distance = 0f;
        var tMin = float.NegativeInfinity;
        var tMax = float.PositiveInfinity;
        for (var axis = 0; axis < 3; axis++)
        {
            var o = GameMath.Get(origin, axis);
            var d = GameMath.Get(direction, axis);
            var min = GameMath.Get(box.Min, axis);
            var max = GameMath.Get(box.Max, axis);
            if (MathF.Abs(d) < 1e-8f)
            {
                if (o < min || o > max)
                    return false;
                continue;
            }

            var t1 = (min - o) / d;
            var t2 = (max - o) / d;
            if (t1 > t2)
                (t1, t2) = (t2, t1);
            tMin = MathF.Max(tMin, t1);
            tMax = MathF.Min(tMax, t2);
            if (tMin > tMax)
                return false;
        }

        if (tMin < 0f)
            return false;

        distance = tMin;
        return true;
    }

    private static bool OverlapsOnOtherAxes(in Box a, in Box b, int axis)
    {
        for (var other = 0; other < 3; other++)
        {
            if (other == axis)
                continue;
            if (GameMath.Get(a.Min, other) >= GameMath.Get(b.Max, other) - Skin ||
                GameMath.Get(a.Max, other) <= GameMath.Get(b.Min, other) + Skin)
                return false;
        }

        return true;
    }

    private static Vector3 ShallowestPush(in Box box, in Box other)
    {
        var best = Vector3.Zero;
        var bestDepth = float.MaxValue;
        for (var axis = 0; axis < 3; axis++)
        {
            var pushPositive = GameMath.Get(other.Max, axis) - GameMath.Get(box.Min, axis);
            var pushNegative = GameMath.Get(other.Min, axis) - GameMath.Get(box.Max, axis);
            var push = MathF.Abs(pushPositive) < MathF.Abs(pushNegative) ? pushPositive : pushNegative;
            if (MathF.Abs(push) < bestDepth)
            {
                bestDepth = MathF.Abs(push);
                best = GameMath.With(Vector3.Zero, axis, push);
            }
        }

        return best;
    }
}
