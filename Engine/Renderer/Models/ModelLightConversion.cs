using System.Numerics;

namespace Engine.Renderer.Models;

public static class ModelLightConversion
{
    private const float DirectionEpsilonSquared = 1e-8f;

    public static bool TryPoint(
        Vector3 diffuse,
        float? fileRange,
        out Vector4 color,
        out float intensity,
        out float range)
    {
        color = default;
        intensity = 0f;
        range = 0f;

        var brightness = Brightness(diffuse);
        if (brightness is not float b)
            return false;

        color = new Vector4(diffuse / b, 1f);
        intensity = global::System.Math.Clamp(MathF.Sqrt(b / 4f), 0.5f, 2f);
        range = fileRange is float file && float.IsFinite(file) && file > 0f
            ? file
            : global::System.Math.Clamp(3f * MathF.Sqrt(b), 1f, 16f);
        return true;
    }

    public static bool TryDirectional(Vector3 diffuse, Vector3 direction, out Vector4 color, out Vector3 baked)
    {
        color = default;
        baked = default;

        var brightness = Brightness(diffuse);
        if (brightness is not float b)
            return false;

        color = b > 1f ? new Vector4(diffuse / b, 1f) : new Vector4(diffuse, 1f);
        baked = BakeDirection(direction);
        return true;
    }

    public static Vector3 BakeDirection(Vector3 direction) =>
        direction.LengthSquared() < DirectionEpsilonSquared
            ? new Vector3(0f, -1f, 0f)
            : Vector3.Normalize(direction);

    private static float? Brightness(Vector3 diffuse)
    {
        var b = MathF.Max(diffuse.X, MathF.Max(diffuse.Y, diffuse.Z));
        return float.IsFinite(b) && b > 0f ? b : null;
    }
}
