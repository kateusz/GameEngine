using System.Numerics;
using ECS;
using ECS.Systems;
using Engine.Physics;
using Math;
using SceneComponents;
using SceneComponents.Physics;
using Serilog;

namespace Engine.Scene.Systems;

/// <summary>
/// Fixed-timestep 3D physics simulation via <see cref="IPhysicsWorld3D"/>.
/// Per-scene system — each 3D scene owns its own physics world instance.
/// </summary>
internal sealed class PhysicsSimulationSystem3D(
    IPhysicsWorld3D physicsWorld,
    IContext context,
    PhysicsRuntimeBodyStore3D bodyStore) : ISystem, IDisposable
{
    private static readonly ILogger Logger = Log.ForContext<PhysicsSimulationSystem3D>();

    private float _physicsAccumulator;
    private bool _disposed;

    private const int MaxPhysicsStepsPerFrame = 5;

    public int Priority => SystemPriorities.PhysicsSimulationSystem;

    public void OnInit()
    {
        _physicsAccumulator = 0f;
        EnsureBodiesCreated();
        Logger.Debug("PhysicsSimulationSystem3D initialized with priority {Priority}", Priority);
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
                 context.View<RigidBody3DComponent, TransformComponent>())
        {
            if (GetColliderMaterial(entity) is not { } material)
                continue;
            if (!bodyStore.TryGet(entity.Id, out var body))
                continue;

            body.UpdateFixtureMaterial(material.Density, material.Friction, material.Restitution);

            transform.Translation = body.Position;
            if (!component.FixedRotation)
            {
                var matrix = MathHelpers.MatrixFromQuaternion(body.Orientation);
                MathHelpers.DecomposeTransform(matrix, out _, out var rotation, out _);
                transform.Rotation = rotation;
            }

            if (component.BodyType is RigidBodyType.Dynamic or RigidBodyType.Kinematic)
                component.Velocity = body.LinearVelocity;
        }
    }

    public void OnShutdown()
    {
        Logger.Debug("PhysicsSimulationSystem3D shutting down - cleaning up physics bodies");

        foreach (var (_, body) in bodyStore.Snapshot())
            physicsWorld.DestroyBody(body);

        bodyStore.Clear();
        Logger.Debug("PhysicsSimulationSystem3D shut down - all physics bodies destroyed");
    }

    private void EnsureBodiesCreated()
    {
        foreach (var (entity, component, transform) in context.View<RigidBody3DComponent, TransformComponent>())
        {
            if (bodyStore.TryGet(entity.Id, out _))
                continue;
            if (!HasCollider(entity))
                continue;

            // FixedRotation: keep editor euler (Mixamo −90) off the sim capsule.
            var orientation = component.FixedRotation
                ? Quaternion.Identity
                : MathHelpers.QuaternionFromEuler(transform.Rotation);
            var body = physicsWorld.CreateBody(new PhysicsBodyDef3D(
                transform.Translation,
                orientation,
                ToMotionType(component.BodyType),
                component.FixedRotation,
                component.GravityScale));

            body.Entity = entity;
            bodyStore.Set(entity.Id, body);

            AttachFixture(entity, body, transform);
        }
    }

    // One fixture per body: Box > Sphere > Capsule.
    private static void AttachFixture(Entity entity, IPhysicsBody3D body, TransformComponent transform)
    {
        var scale = transform.Scale;

        if (entity.TryGetComponent<BoxCollider3DComponent>(out var box))
        {
            body.CreateBoxFixture(new PhysicsBoxFixtureDef3D(
                new Vector3(
                    box.Size.X * MathF.Abs(scale.X),
                    box.Size.Y * MathF.Abs(scale.Y),
                    box.Size.Z * MathF.Abs(scale.Z)),
                box.Offset * scale,
                box.Density,
                box.Friction,
                box.Restitution,
                box.IsTrigger));
            return;
        }

        if (entity.TryGetComponent<SphereCollider3DComponent>(out var sphere))
        {
            var radiusScale = (MathF.Abs(scale.X) + MathF.Abs(scale.Y) + MathF.Abs(scale.Z)) / 3f;
            body.CreateSphereFixture(new PhysicsSphereFixtureDef3D(
                sphere.Radius * radiusScale,
                sphere.Offset * scale,
                sphere.Density,
                sphere.Friction,
                sphere.Restitution,
                sphere.IsTrigger));
            return;
        }

        if (entity.TryGetComponent<CapsuleCollider3DComponent>(out var capsule))
        {
            var radiusScale = (MathF.Abs(scale.X) + MathF.Abs(scale.Z)) * 0.5f;
            body.CreateCapsuleFixture(new PhysicsCapsuleFixtureDef3D(
                capsule.Radius * radiusScale,
                capsule.Length * MathF.Abs(scale.Y),
                capsule.Offset * scale,
                capsule.Density,
                capsule.Friction,
                capsule.Restitution,
                capsule.IsTrigger));
        }
    }

    private static bool HasCollider(Entity entity) =>
        entity.HasComponent<BoxCollider3DComponent>()
        || entity.HasComponent<SphereCollider3DComponent>()
        || entity.HasComponent<CapsuleCollider3DComponent>();

    private static (float Density, float Friction, float Restitution)? GetColliderMaterial(Entity entity)
    {
        if (entity.TryGetComponent<BoxCollider3DComponent>(out var box))
            return (box.Density, box.Friction, box.Restitution);
        if (entity.TryGetComponent<SphereCollider3DComponent>(out var sphere))
            return (sphere.Density, sphere.Friction, sphere.Restitution);
        if (entity.TryGetComponent<CapsuleCollider3DComponent>(out var capsule))
            return (capsule.Density, capsule.Friction, capsule.Restitution);
        return null;
    }

    private void SyncVelocitiesToBodies()
    {
        foreach (var (entity, component) in context.View<RigidBody3DComponent>())
        {
            if (component.BodyType is not (RigidBodyType.Dynamic or RigidBodyType.Kinematic))
                continue;

            if (bodyStore.TryGet(entity.Id, out var body))
                body.LinearVelocity = component.Velocity;
        }
    }

    private void SyncKinematicTransformsToBodies()
    {
        foreach (var (entity, component, transform) in context.View<RigidBody3DComponent, TransformComponent>())
        {
            if (component.BodyType != RigidBodyType.Kinematic)
                continue;

            if (!bodyStore.TryGet(entity.Id, out var body))
                continue;

            body.Position = transform.Translation;
            if (!component.FixedRotation)
                body.Orientation = MathHelpers.QuaternionFromEuler(transform.Rotation);
        }
    }

    private void CleanupOrphanedBodies()
    {
        var activeEntityIds = context.View<RigidBody3DComponent>().Select(v => v.Entity.Id).ToHashSet();
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
        Logger.Debug("PhysicsSimulationSystem3D disposed");
    }
}
