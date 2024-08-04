using System.Numerics;

namespace Engine.Math;

public static class MathHelpers
{
    public const float RadToDegFactor = 180.0f / MathF.PI;
    public const float DegToRadFactor = MathF.PI / 180.0f;
    
    public static float RadiansToDegrees(float radians)
    {
        return radians * RadToDegFactor;
    }

    public static float DegreesToRadians(float degrees)
    {
        return degrees * DegToRadFactor;
    }
    
    public static Vector3 ToDegrees(Vector3 radians)
    {
        return new Vector3(
            RadiansToDegrees(radians.X),
            RadiansToDegrees(radians.Y),
            RadiansToDegrees(radians.Z)
        );
    }

    public static Vector3 ToRadians(Vector3 degrees)
    {
        return new Vector3(
            DegreesToRadians(degrees.X),
            DegreesToRadians(degrees.Y),
            DegreesToRadians(degrees.Z)
        );
    }
}