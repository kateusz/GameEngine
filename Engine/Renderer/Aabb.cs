using System.Numerics;
using Engine.Renderer.Meshes;

namespace Engine.Renderer;

internal readonly record struct Aabb(Vector3 Min, Vector3 Max)
{
    public const float UnitCubeHalfExtent = 0.5f;

    public static Aabb UnitCube { get; } = new(
        new Vector3(-UnitCubeHalfExtent),
        new Vector3(UnitCubeHalfExtent));

    public static Aabb? FromPositions(List<Mesh.Vertex> vertices)
    {
        if (vertices.Count == 0)
            return null;

        var min = new Vector3(float.PositiveInfinity);
        var max = new Vector3(float.NegativeInfinity);
        foreach (var vertex in vertices)
        {
            var position = vertex.Position;
            if (!float.IsFinite(position.X) || !float.IsFinite(position.Y) || !float.IsFinite(position.Z))
                return null;

            min = Vector3.Min(min, position);
            max = Vector3.Max(max, position);
        }

        return new Aabb(min, max);
    }

    internal static void TransformCorners(Matrix4x4 world, Aabb local, Span<Vector3> corners)
    {
        var index = 0;
        for (var z = 0; z < 2; z++)
        for (var y = 0; y < 2; y++)
        for (var x = 0; x < 2; x++)
        {
            var corner = new Vector3(
                x == 0 ? local.Min.X : local.Max.X,
                y == 0 ? local.Min.Y : local.Max.Y,
                z == 0 ? local.Min.Z : local.Max.Z);
            corners[index++] = Vector3.Transform(corner, world);
        }
    }

    internal static bool ContainsPoint(Vector3 point, Matrix4x4 world, Aabb local) =>
        ClosestPointDistanceSquared(point, world, local) <= 0f;

    internal static bool ContainsPoint(Vector3 point, Aabb worldBounds) =>
        ClosestPointDistanceSquared(point, worldBounds) <= 0f;

    internal static float ClosestPointDistanceSquared(Vector3 point, Aabb worldBounds)
    {
        var closest = Vector3.Clamp(point, worldBounds.Min, worldBounds.Max);
        return Vector3.DistanceSquared(point, closest);
    }

    internal static float ClosestPointDistanceSquared(Vector3 point, Matrix4x4 world, Aabb local)
    {
        WorldMinMax(world, local, out var min, out var max);
        var closest = new Vector3(
            System.Math.Clamp(point.X, min.X, max.X),
            System.Math.Clamp(point.Y, min.Y, max.Y),
            System.Math.Clamp(point.Z, min.Z, max.Z));
        return Vector3.DistanceSquared(point, closest);
    }

    internal static void WorldMinMax(Matrix4x4 world, Aabb local, out Vector3 min, out Vector3 max)
    {
        // Affine box transform without 8 corner transforms: world extent = |M| * local half-extent.
        // Abs also normalizes inverted input boxes (zone bounds are authored in any corner order).
        var center = (local.Min + local.Max) * 0.5f;
        var extent = new Vector3(
            MathF.Abs(local.Max.X - local.Min.X),
            MathF.Abs(local.Max.Y - local.Min.Y),
            MathF.Abs(local.Max.Z - local.Min.Z)) * 0.5f;
        var worldCenter = Vector3.Transform(center, world);
        var worldExtent = new Vector3(
            MathF.Abs(world.M11) * extent.X + MathF.Abs(world.M21) * extent.Y + MathF.Abs(world.M31) * extent.Z,
            MathF.Abs(world.M12) * extent.X + MathF.Abs(world.M22) * extent.Y + MathF.Abs(world.M32) * extent.Z,
            MathF.Abs(world.M13) * extent.X + MathF.Abs(world.M23) * extent.Y + MathF.Abs(world.M33) * extent.Z);
        min = worldCenter - worldExtent;
        max = worldCenter + worldExtent;
    }
}
