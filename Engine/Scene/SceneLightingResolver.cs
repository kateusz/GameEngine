using System.Numerics;
using ECS;
using Engine.Renderer;
using SceneComponents;
using SceneComponents.Lighting;

namespace Engine.Scene;

internal static class SceneLightingResolver
{
    public static SceneLighting Resolve(IContext context)
    {
        var (ambientColor, ambientStrength) = ResolveAmbient(context);
        var directional = ResolveDirectional(context);
        var shadowLightSpace = TryBuildShadowLightSpace(directional);
        return new SceneLighting(
            ambientColor,
            ambientStrength,
            directional.Direction,
            directional.Color,
            shadowLightSpace);
    }

    private static (Vector3 Color, float Strength) ResolveAmbient(IContext context)
    {
        foreach (var (_, alc) in context.View<AmbientLightComponent>())
            return (new Vector3(alc.Color.X, alc.Color.Y, alc.Color.Z), alc.Strength);

        return (SceneLighting.Default.AmbientColor, SceneLighting.Default.AmbientStrength);
    }

    private static DirectionalLightState ResolveDirectional(IContext context)
    {
        foreach (var (entity, dlc) in context.View<DirectionalLightComponent>())
        {
            var origin = Vector3.Zero;
            if (entity.TryGetComponent<TransformComponent>(out var transform))
            {
                var world = transform.GetWorldTransform();
                origin = new Vector3(world.M41, world.M42, world.M43);
            }

            return new DirectionalLightState
            {
                Found = true,
                Direction = LightingMath.NormalizeDirection(dlc.Direction),
                Color = new Vector3(dlc.Color.X, dlc.Color.Y, dlc.Color.Z),
                Origin = origin,
                OrthoSize = dlc.OrthoSize
            };
        }

        return new DirectionalLightState
        {
            Found = false,
            Direction = SceneLighting.Default.DirectionalDirection,
            Color = SceneLighting.Default.DirectionalColor
        };
    }

    private static Matrix4x4? TryBuildShadowLightSpace(in DirectionalLightState light)
    {
        if (!light.Found || light.OrthoSize <= 0f || light.Color.LengthSquared() < 1e-10f)
            return null;

        var matrix = LightSpaceMatrix.Create(light.Direction, light.Origin, light.OrthoSize);
        return LightSpaceMatrix.IsFinite(matrix) ? matrix : null;
    }

    private readonly struct DirectionalLightState
    {
        public bool Found { get; init; }
        public Vector3 Direction { get; init; }
        public Vector3 Color { get; init; }
        public Vector3 Origin { get; init; }
        public float OrthoSize { get; init; }
    }
}
