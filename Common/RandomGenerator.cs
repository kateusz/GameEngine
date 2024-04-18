namespace Common;

public class RandomGenerator
{
    private static readonly Random Random = new();

    public static float GenerateRandomFloat(float minValue, float maxValue)
    {
        return (float)(Random.NextDouble() * (maxValue - minValue) + minValue);
    }
}