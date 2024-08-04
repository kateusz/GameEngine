using System.Numerics;

namespace Engine.Math;

public static class MathHelpers
{
    public static float ToRadians(float degrees)
    {
        return (float)(degrees * (System.Math.PI / 180));
    }
    
    public static float ToDegrees(float radians)
    {
        return radians * (MathF.PI / 180.0f);;
    }
    
    public static Vector3 ToDegrees(Vector3 radians)
    {
        return new Vector3(
            ToDegrees(radians.X),
            ToDegrees(radians.Y),
            ToDegrees(radians.Z)
        );
    }

    public static Vector3 ToRadians(Vector3 degrees)
    {
        return new Vector3(
            ToRadians(degrees.X),
            ToRadians(degrees.Y),
            ToRadians(degrees.Z)
        );
    }
}