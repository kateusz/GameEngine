using System.Numerics;
using ECS;
using SceneComponents;
using SceneComponents.Physics;
using SceneComponents.Rendering;
using Scripting;
using Serilog;

namespace Editor.Features.Tiled;

public static class TiledMapPicking
{
    private static readonly ILogger Logger = Log.ForContext(typeof(TiledMapPicking));

    public static Entity? Resolve(IEntityHierarchy hierarchy, Entity? gpuHit, Vector2 world) =>
        gpuHit is null || !gpuHit.HasComponent<TileMapComponent>()
            ? gpuHit
            : PickChildAt(hierarchy, gpuHit, world) ?? gpuHit;

    public static void LogClick(IEntityHierarchy hierarchy, Entity? gpuHit, Vector2 world)
    {
        if (gpuHit is null || !gpuHit.HasComponent<TileMapComponent>())
        {
            Logger.Information("[TilePick] gpuHit={Hit} world=({X:0.###},{Y:0.###})",
                gpuHit is null ? "null" : $"{gpuHit.Id} '{gpuHit.Name}'", world.X, world.Y);
            return;
        }

        var picked = PickChildAt(hierarchy, gpuHit, world);
        FindNearest(hierarchy, gpuHit, world, out var nearest, out var dist, out var nearestT);
        Logger.Information(
            "[TilePick] map={Map} world=({X:0.###},{Y:0.###}) children={Count} picked={Picked} nearest={Nearest} dist={Dist:0.###} nearestT=({Nx:0.###},{Ny:0.###})",
            $"{gpuHit.Id} '{gpuHit.Name}'", world.X, world.Y,
            CountChildren(hierarchy, gpuHit),
            picked is null ? "none" : $"{picked.Id} '{picked.Name}'",
            nearest is null ? "none" : $"{nearest.Id} '{nearest.Name}'",
            dist, nearestT.X, nearestT.Y);
    }

    private static Entity? PickChildAt(IEntityHierarchy hierarchy, Entity map, Vector2 world)
    {
        Entity? best = null;
        var bestArea = float.MaxValue;
        Walk(hierarchy, map, world, ref best, ref bestArea);
        if (best is not null)
            return best;

        // Polyline/point markers are a 1×1 at one vertex; painted tiles around them are not entities.
        FindNearest(hierarchy, map, world, out var nearest, out _, out _);
        return nearest;
    }

    private static int CountChildren(IEntityHierarchy hierarchy, Entity parent)
    {
        var n = 0;
        foreach (var child in hierarchy.GetChildren(parent))
            n += 1 + CountChildren(hierarchy, child);
        return n;
    }

    private static void FindNearest(
        IEntityHierarchy hierarchy, Entity parent, Vector2 world,
        out Entity? nearest, out float dist, out Vector2 nearestT)
    {
        nearest = null;
        dist = float.MaxValue;
        nearestT = default;
        WalkNearest(hierarchy, parent, world, ref nearest, ref dist, ref nearestT);
    }

    private static void WalkNearest(
        IEntityHierarchy hierarchy, Entity parent, Vector2 world,
        ref Entity? nearest, ref float dist, ref Vector2 nearestT)
    {
        foreach (var child in hierarchy.GetChildren(parent))
        {
            if (IsPickable(child))
            {
                var p = hierarchy.GetWorldPosition(child);
                var d = Vector2.Distance(world, new Vector2(p.X, p.Y));
                if (d < dist)
                {
                    nearest = child;
                    dist = d;
                    nearestT = new Vector2(p.X, p.Y);
                }
            }

            WalkNearest(hierarchy, child, world, ref nearest, ref dist, ref nearestT);
        }
    }

    private static void Walk(
        IEntityHierarchy hierarchy, Entity parent, Vector2 world, ref Entity? best, ref float bestArea)
    {
        foreach (var child in hierarchy.GetChildren(parent))
        {
            if (TryHitArea(child, world, out var area) && area < bestArea)
            {
                best = child;
                bestArea = area;
            }

            Walk(hierarchy, child, world, ref best, ref bestArea);
        }
    }

    private static bool TryHitArea(Entity entity, Vector2 world, out float area)
    {
        area = 0f;
        if (!IsPickable(entity) || !entity.TryGetComponent<TransformComponent>(out var transform))
            return false;

        var matrix = transform.GetWorldTransform();
        if (!Matrix4x4.Invert(matrix, out var inverse))
            return false;

        var local = Vector3.Transform(new Vector3(world.X, world.Y, 0f), inverse);

        if (entity.TryGetComponent<BoxCollider2DComponent>(out var box))
        {
            if (MathF.Abs(local.X - box.Offset.X) > box.Size.X || MathF.Abs(local.Y - box.Offset.Y) > box.Size.Y)
                return false;
            area = MathF.Max(1e-6f, box.Size.X * box.Size.Y * 4f);
            return true;
        }

        if (MathF.Abs(local.X) > 0.5f || MathF.Abs(local.Y) > 0.5f)
            return false;
        area = MathF.Max(1e-6f, MathF.Abs(transform.Scale.X * transform.Scale.Y));
        return true;
    }

    private static bool IsPickable(Entity entity) =>
        entity.HasComponent<TiledObjectComponent>()
        || entity.HasComponent<BoxCollider2DComponent>()
        || entity.HasComponent<SubTextureRendererComponent>()
        || entity.HasComponent<SpriteRendererComponent>();
}
