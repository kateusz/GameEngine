using System.Numerics;
using ECS;
using SceneComponents.Lighting;

namespace Engine.Scene;

internal static class SceneLightingResolver
{
    public static SceneLighting Resolve(IContext context)
    {
        var (ambientColor, ambientStrength) = ResolveAmbient(context);
        var (direction, color) = ResolveDirectional(context);
        return new SceneLighting(ambientColor, ambientStrength, direction, color);
    }

    private static (Vector3 Color, float Strength) ResolveAmbient(IContext context)
    {
        foreach (var (_, alc) in context.View<AmbientLightComponent>())
            return (new Vector3(alc.Color.X, alc.Color.Y, alc.Color.Z), alc.Strength);

        return (SceneLighting.Default.AmbientColor, SceneLighting.Default.AmbientStrength);
    }

    private static (Vector3 Direction, Vector3 Color) ResolveDirectional(IContext context)
    {
        foreach (var (_, dlc) in context.View<DirectionalLightComponent>())
            return (NormalizeDirection(dlc.Direction), new Vector3(dlc.Color.X, dlc.Color.Y, dlc.Color.Z));

        return (SceneLighting.Default.DirectionalDirection, SceneLighting.Default.DirectionalColor);
    }

    private static Vector3 NormalizeDirection(Vector3 direction) =>
        direction.LengthSquared() < 1e-6f ? new Vector3(0, -1, 0) : Vector3.Normalize(direction);
}
