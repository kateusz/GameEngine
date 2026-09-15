using System.Numerics;

namespace Engine.Renderer;

internal static class LightingMath
{
    public const float DirectionEpsilon = 1e-6f;
    public static readonly Vector3 DefaultDirection = new(0, -1, 0);

    public static Vector3 NormalizeDirection(Vector3 direction) =>
        direction.LengthSquared() < DirectionEpsilon ? DefaultDirection : Vector3.Normalize(direction);
}
