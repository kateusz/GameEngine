using System.Numerics;

namespace Engine.Renderer;

internal static class LightingMath
{
    public const float DirectionEpsilon = 1e-6f;
    public const int MaxPointLights = 4; // cube.frag / modelShader.frag u_PointLights[]
    public const int MaxSpotLights = 2; // cube.frag / modelShader.frag u_SpotLights[]
    public static readonly Vector3 DefaultDirection = new(0, -1, 0);
    public static readonly Vector3 DefaultForward = new(0, 0, -1);

    public static Vector3 NormalizeDirection(Vector3 direction) =>
        NormalizeDirection(direction, DefaultDirection);

    public static Vector3 NormalizeDirection(Vector3 direction, Vector3 fallback) =>
        direction.LengthSquared() < DirectionEpsilon ? fallback : Vector3.Normalize(direction);
}
