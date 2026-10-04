using System.Numerics;

namespace Engine.Renderer;

internal static class LightingMath
{
    private const float DirectionEpsilon = 1e-6f;
    public static readonly Vector3 DefaultDirection = new(0, -1, 0);
    public const int MaxPointLights = 8;
    public const float PointLightDistanceEpsilon = 0.0001f;

    public static Vector3 NormalizeDirection(Vector3 direction) =>
        direction.LengthSquared() < DirectionEpsilon ? DefaultDirection : Vector3.Normalize(direction);
    
    public static float PointAttenuation(float distance, float range)
    {
        if (range <= 0f || distance >= range)
            return 0f;
        var remaining = 1f - distance / range;
        return remaining * remaining;
    }
}