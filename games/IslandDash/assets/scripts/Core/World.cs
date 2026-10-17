using ECS;
using SceneComponents;
using SceneComponents.Camera;

namespace IslandDash;

/// <summary>System execution order. All game systems run before the engine's transform hierarchy pass (115) and render (150).</summary>
public static class SystemOrder
{
    public const int Level = 101;
    public const int Platforms = 102;
    public const int Player = 104;
    public const int Enemies = 105;
    public const int Interactions = 106;
    public const int Flow = 108;
    public const int Camera = 110;
    public const int Animation = 112;
    public const int Hud = 113;

    /// <summary>Clamp so a hitch (window drag, asset load) can't produce a giant simulation step.</summary>
    public const float MaxDelta = 1f / 20f;

    public static float ClampDelta(TimeSpan deltaTime) => MathF.Min((float)deltaTime.TotalSeconds, MaxDelta);
}

/// <summary>Lookups shared by systems. Everything is found through components; systems keep no cross-frame state.</summary>
public static class World
{
    public static Entity? GameEntity(Context context)
    {
        foreach (var (entity, _) in context.View<GameSettingsComponent>())
            return entity;
        return null;
    }

    public static GameStateComponent? State(Context context)
    {
        foreach (var (_, state) in context.View<GameStateComponent>())
            return state;
        return null;
    }

    public static Entity? Player(Context context)
    {
        foreach (var (entity, _) in context.View<PlayerComponent>())
            return entity;
        return null;
    }

    public static Entity? PrimaryCamera(Context context)
    {
        foreach (var (entity, camera) in context.View<CameraComponent>())
        {
            if (camera.Primary)
                return entity;
        }

        return null;
    }

    public static T Get<T>(Context context) where T : class, ECS.IComponent, new()
    {
        foreach (var (_, component) in context.View<T>())
            return component;
        return new T();
    }

    public static Box PlayerBox(Entity player)
    {
        var transform = player.GetComponent<TransformComponent>();
        var collider = player.GetComponent<ColliderComponent>();
        return Collision.WorldBox(transform, collider);
    }
}
