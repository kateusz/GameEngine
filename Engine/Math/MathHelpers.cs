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
}