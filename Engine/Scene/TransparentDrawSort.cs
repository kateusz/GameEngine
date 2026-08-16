using System.Numerics;

namespace Engine.Scene;

/// <summary>Back-to-front sort for transparent forward draws (painter's algorithm).</summary>
internal static class TransparentDrawSort
{
    internal static void SortBackToFront<T>(List<T> items, Vector3 cameraPosition, Func<T, Vector3> worldPosition)
    {
        items.Sort((a, b) =>
            DistanceSquared(cameraPosition, worldPosition(b))
                .CompareTo(DistanceSquared(cameraPosition, worldPosition(a))));
    }

    internal static float DistanceSquared(Vector3 cameraPosition, Vector3 worldPosition)
    {
        var delta = worldPosition - cameraPosition;
        return delta.LengthSquared();
    }
}
