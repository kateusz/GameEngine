using ECS;
using SceneComponents;
using SceneComponents.Rendering;

namespace IslandDash;

/// <summary>Marks entities created at runtime by the level builder so a restart can remove them.</summary>
public sealed class LevelEntityComponent : IGameComponent
{
    public IComponent Clone() => new LevelEntityComponent();
}

/// <summary>
/// Runtime entity creation. Scripts only get the scene's <see cref="Context"/> (no IScene.CreateEntity), so ids are
/// allocated from a high range that scene files never use.
/// </summary>
public static class Spawner
{
    private const int FirstRuntimeId = 1_000_000;
    private static int _nextId = FirstRuntimeId;

    public static Entity Create(Context context, string name, Vector3 position, bool levelOwned = true)
    {
        while (context.Contains(_nextId))
            _nextId++;

        var entity = new Entity(_nextId++, name);
        entity.AddComponent(new TransformComponent(position, Vector3.Zero, Vector3.One));
        if (levelOwned)
            entity.AddComponent(new LevelEntityComponent());
        context.Register(entity);
        return entity;
    }

    /// <summary>Static model placed once (ground tiles, decoration). Animated models use <see cref="ProceduralAnimatorComponent"/>.</summary>
    public static Entity CreateModel(Context context, string name, ModelEntry model, Vector3 position, Vector3 sizeScale, float yaw = 0f, bool levelOwned = true)
    {
        var entity = Create(context, name, position, levelOwned);
        entity.AddComponent(model.CreateRenderer());
        var scale = model.Scale * sizeScale;
        var rotation = GameMath.YawRotation(yaw);
        var transform = entity.GetComponent<TransformComponent>();
        transform.Translation = position + Vector3.Transform(model.Pivot * scale, rotation);
        transform.Rotation = GameMath.ToEngineEuler(rotation);
        transform.Scale = scale;
        return entity;
    }

    /// <summary>Visual entity driven every frame by the owner's <see cref="ProceduralAnimatorComponent"/>.</summary>
    /// <param name="ownerToModelBase">World offset from the owner's origin to where the model's pivot should sit.</param>
    public static ProceduralAnimatorComponent AttachAnimatedModel(
        Context context, Entity owner, ModelEntry model, AnimationDriverKind driver, Vector3 sizeScale,
        float yaw = 0f, Vector3 ownerToModelBase = default)
    {
        var position = owner.GetComponent<TransformComponent>().Translation;
        var visual = Create(context, owner.Name + "_Visual", position, owner.HasComponent<LevelEntityComponent>());
        visual.AddComponent(model.CreateRenderer());

        var animator = new ProceduralAnimatorComponent
        {
            VisualEntityId = visual.Id,
            Driver = driver,
            RestScale = model.Scale * sizeScale,
            RestOffset = model.Pivot * model.Scale * sizeScale + ownerToModelBase,
            RestYaw = yaw,
            FacingYaw = yaw,
            Yaw = yaw,
            Phase = GameMath.Hash01(owner.Id) * MathF.Tau
        };
        owner.AddComponent(animator);
        return animator;
    }

    public static void SetModel(Entity visual, ModelEntry model)
    {
        if (visual.TryGetComponent<ModelRendererComponent>(out var renderer))
            model.Apply(renderer);
    }
}
