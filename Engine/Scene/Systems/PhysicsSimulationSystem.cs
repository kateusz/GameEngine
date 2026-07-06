using System.Numerics;
using ECS;
using ECS.Systems;
using Engine.Physics;
using SceneComponents;
using SceneComponents.Physics;
using Serilog;

namespace Engine.Scene.Systems;

/// <summary>
/// Fixed-timestep 2D physics simulation via <see cref="IPhysicsWorld2D"/>.
/// Per-scene system — each scene owns its own physics world instance.
/// </summary>
internal sealed class PhysicsSimulationSystem(
    IPhysicsWorld2D physicsWorld,
    IContext context,
    PhysicsRuntimeBodyStore bodyStore) : ISystem, IDisposable
{
    private static readonly ILogger Logger = Log.ForContext<PhysicsSimulationSystem>();

    private float _physicsAccumulator;
    private bool _disposed;

    private const int MaxPhysicsStepsPerFrame = 5;

    public int Priority => SystemPriorities.PhysicsSimulationSystem;

    public void OnInit()
    {
        _physicsAccumulator = 0f;
        EnsureBodiesCreated();
        Logger.Debug("PhysicsSimulationSystem initialized with priority {Priority}", Priority);
    }

    public void OnUpdate(TimeSpan deltaTime)
    {
        const int velocityIterations = 6;
        const int positionIterations = 2;
        var deltaSeconds = (float)deltaTime.TotalSeconds;

        _physicsAccumulator += deltaSeconds;

        EnsureBodiesCreated();
        CleanupOrphanedBodies();

        var stepCount = 0;
        while (_physicsAccumulator >= PhysicsConstants.PhysicsTimestep && stepCount < MaxPhysicsStepsPerFrame)
        {
            SyncKinematicTransformsToBodies();
            SyncVelocitiesToBodies();
            physicsWorld.Step(PhysicsConstants.PhysicsTimestep, velocityIterations, positionIterations);
            _physicsAccumulator -= PhysicsConstants.PhysicsTimestep;
            stepCount++;
        }

        if (_physicsAccumulator >= PhysicsConstants.PhysicsTimestep)
            _physicsAccumulator = PhysicsConstants.PhysicsTimestep * 0.5f;

        foreach (var (entity, component, transform) in
                 context.View<RigidBody2DComponent, TransformComponent>())
        {
            if (!entity.TryGetComponent<BoxCollider2DComponent>(out var collision))
                continue;
            if (!bodyStore.TryGet(entity.Id, out var body))
                continue;

            body.UpdateFixtureMaterial(collision.Density, collision.Friction, collision.Restitution);

            var position = body.Position;
            transform.Translation = new Vector3(position.X, position.Y, 0);
            transform.Rotation = transform.Rotation with { Z = body.Angle };

            if (component.BodyType is RigidBodyType.Dynamic or RigidBodyType.Kinematic)
                component.Velocity = body.LinearVelocity;
        }
    }

    public void OnShutdown()
    {
        Logger.Debug("PhysicsSimulationSystem shutting down - cleaning up physics bodies");

        foreach (var (_, body) in bodyStore.Snapshot())
            physicsWorld.DestroyBody(body);

        bodyStore.Clear();
        Logger.Debug("PhysicsSimulationSystem shut down - all physics bodies destroyed");
    }

    private void EnsureBodiesCreated()
    {
        foreach (var (entity, component, transform) in context.View<RigidBody2DComponent, TransformComponent>())
        {
            if (bodyStore.TryGet(entity.Id, out _))
                continue;

            var body = physicsWorld.CreateBody(new PhysicsBodyDef(
                new Vector2(transform.Translation.X, transform.Translation.Y),
                transform.Rotation.Z,
                ToMotionType(component.BodyType),
                component.FixedRotation,
                component.GravityScale));

            body.Entity = entity;
            bodyStore.Set(entity.Id, body);

            if (!entity.HasComponent<BoxCollider2DComponent>())
                continue;

            var boxCollider = entity.GetComponent<BoxCollider2DComponent>();
            body.CreateBoxFixture(new PhysicsBoxFixtureDef(
                boxCollider.Size.X * transform.Scale.X,
                boxCollider.Size.Y * transform.Scale.Y,
                new Vector2(boxCollider.Offset.X * transform.Scale.X, boxCollider.Offset.Y * transform.Scale.Y),
                boxCollider.Density,
                boxCollider.Friction,
                boxCollider.Restitution,
                boxCollider.IsTrigger));
        }
    }

    private void SyncVelocitiesToBodies()
    {
        foreach (var (entity, component) in context.View<RigidBody2DComponent>())
        {
            if (component.BodyType is not (RigidBodyType.Dynamic or RigidBodyType.Kinematic))
                continue;

            if (bodyStore.TryGet(entity.Id, out var body))
                body.LinearVelocity = component.Velocity;
        }
    }

    private void SyncKinematicTransformsToBodies()
    {
        foreach (var (entity, component, transform) in context.View<RigidBody2DComponent, TransformComponent>())
        {
            if (component.BodyType != RigidBodyType.Kinematic)
                continue;

            if (!bodyStore.TryGet(entity.Id, out var body))
                continue;

            body.Position = new Vector2(transform.Translation.X, transform.Translation.Y);
            body.Angle = transform.Rotation.Z;
        }
    }

    private void CleanupOrphanedBodies()
    {
        var activeEntityIds = context.View<RigidBody2DComponent>().Select(v => v.Entity.Id).ToHashSet();
        var staleEntityIds = bodyStore.Snapshot().Keys.Where(id => !activeEntityIds.Contains(id)).ToList();
        foreach (var staleEntityId in staleEntityIds)
        {
            if (!bodyStore.TryGet(staleEntityId, out var body))
                continue;

            physicsWorld.DestroyBody(body);
            bodyStore.Remove(staleEntityId);
        }
    }

    private static PhysicsBodyMotionType ToMotionType(RigidBodyType bodyType) =>
        bodyType switch
        {
            RigidBodyType.Static => PhysicsBodyMotionType.Static,
            RigidBodyType.Dynamic => PhysicsBodyMotionType.Dynamic,
            RigidBodyType.Kinematic => PhysicsBodyMotionType.Kinematic,
            _ => throw new ArgumentOutOfRangeException(nameof(bodyType), bodyType, null)
        };

    public void Dispose()
    {
        if (_disposed)
            return;

        physicsWorld.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
        Logger.Debug("PhysicsSimulationSystem disposed");
    }
}
