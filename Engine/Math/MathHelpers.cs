namespace Engine.Math;

public static class MathHelpers
{
    public static float ToRadians(float degrees)
    {
        return (float)(degrees * (System.Math.PI / 180));
    }
}