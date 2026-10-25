namespace IslandDash;

/// <summary>Axis-aligned bounding box in world space.</summary>
public readonly record struct Box(Vector3 Min, Vector3 Max)
{
    public static Box FromCenter(Vector3 center, Vector3 halfExtents) => new(center - halfExtents, center + halfExtents);

    public Vector3 Center => (Min + Max) * 0.5f;

    public Box Translate(Vector3 delta) => new(Min + delta, Max + delta);

    /// <summary>Strict overlap: boxes that only touch (within <paramref name="epsilon"/>) do not overlap.</summary>
    public bool Overlaps(in Box other, float epsilon = 0f) =>
        Min.X < other.Max.X - epsilon && Max.X > other.Min.X + epsilon &&
        Min.Y < other.Max.Y - epsilon && Max.Y > other.Min.Y + epsilon &&
        Min.Z < other.Max.Z - epsilon && Max.Z > other.Min.Z + epsilon;

    public bool Contains(Vector3 point) =>
        point.X >= Min.X && point.X <= Max.X &&
        point.Y >= Min.Y && point.Y <= Max.Y &&
        point.Z >= Min.Z && point.Z <= Max.Z;

    public Vector3 ClosestPoint(Vector3 point) => Vector3.Clamp(point, Min, Max);

    public float DistanceSquaredTo(Vector3 point) => Vector3.DistanceSquared(point, ClosestPoint(point));
}
