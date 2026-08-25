using System.Numerics;
using ECS;
using Engine.Renderer;
using Math;
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
        var points = ResolvePointLights(context);
        var spots = ResolveSpotLights(context);
        return new SceneLighting(
            ambientColor,
            ambientStrength,
            directional.Direction,
            directional.Color,
            shadowLightSpace,
            points,
            spots);
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
            return new DirectionalLightState
            {
                Found = true,
                Direction = LightingMath.NormalizeDirection(dlc.Direction),
                Color = Rgb(dlc.Color),
                Origin = WorldPosition(entity),
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

    private static PointLightUniform[]? ResolvePointLights(IContext context)
    {
        PointLightUniform[]? lights = null;
        var count = 0;
        foreach (var (entity, plc) in context.View<PointLightComponent>())
        {
            lights ??= new PointLightUniform[LightingMath.MaxPointLights];
            lights[count++] = new PointLightUniform(
                WorldPosition(entity),
                Rgb(plc.Color),
                plc.Constant,
                plc.Linear,
                plc.Quadratic,
                plc.Range);
            if (count == LightingMath.MaxPointLights)
                break;
        }

        return Trim(lights, count);
    }

    private static SpotLightUniform[]? ResolveSpotLights(IContext context)
    {
        SpotLightUniform[]? lights = null;
        var count = 0;
        foreach (var (entity, slc) in context.View<SpotLightComponent>())
        {
            lights ??= new SpotLightUniform[LightingMath.MaxSpotLights];
            lights[count++] = new SpotLightUniform(
                WorldPosition(entity),
                ResolveSpotAim(entity, slc.Direction),
                Rgb(slc.Color),
                slc.Constant,
                slc.Linear,
                slc.Quadratic,
                MathF.Cos(MathHelpers.DegreesToRadians(slc.InnerCutoff)),
                MathF.Cos(MathHelpers.DegreesToRadians(slc.OuterCutoff)));
            if (count == LightingMath.MaxSpotLights)
                break;
        }

        return Trim(lights, count);
    }

    private static Vector3 ResolveSpotAim(Entity entity, Vector3 localDirection)
    {
        var local = LightingMath.NormalizeDirection(localDirection, LightingMath.DefaultForward);
        if (!entity.TryGetComponent<TransformComponent>(out var transform))
            return local;

        var world = Vector3.TransformNormal(local, transform.GetWorldTransform());
        return LightingMath.NormalizeDirection(world, local);
    }

    private static Vector3 WorldPosition(Entity entity)
    {
        if (!entity.TryGetComponent<TransformComponent>(out var transform))
            return Vector3.Zero;
        var world = transform.GetWorldTransform();
        return new Vector3(world.M41, world.M42, world.M43);
    }

    private static Vector3 Rgb(Vector4 color) => new(color.X, color.Y, color.Z);

    private static T[]? Trim<T>(T[]? buffer, int count)
    {
        if (count == 0)
            return null;
        if (count == buffer!.Length)
            return buffer;
        var trimmed = new T[count];
        Array.Copy(buffer, trimmed, count);
        return trimmed;
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
