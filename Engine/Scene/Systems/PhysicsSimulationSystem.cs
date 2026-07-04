using System.Numerics;
using Box2D.NetStandard.Collision.Shapes;
using Box2D.NetStandard.Dynamics.Bodies;
using Box2D.NetStandard.Dynamics.Fixtures;
using Box2D.NetStandard.Dynamics.World;
using ECS;
using ECS.Systems;
using Engine.Renderer.Cameras;
using SceneComponents;
using SceneComponents.Physics;
using Serilog;

namespace Engine.Scene.Systems;

/// <summary>
/// System responsible for physics simulation using Box2D.
/// Handles fixed timestep physics stepping and synchronization between physics bodies and transforms.
/// This is a PER-SCENE system - each scene has its own instance with its own physics world.
/// </summary>
internal sealed class PhysicsSimulationSystem(
    World physicsWorld,
    IContext context,
    PhysicsRuntimeBodyStore bodyStore) : ISystem, IDisposable
{
    private static readonly ILogger Logger = Log.ForContext<PhysicsSimulationSystem>();

    // Fixed timestep accumulator for deterministic physics
    private float _physicsAccumulator;
    private bool _disposed;

    /// <summary>
    /// Maximum physics steps per frame to prevent spiral of death.
    /// At 60Hz physics with 16ms frames, this allows catching up from frame spikes up to ~83ms.
    /// Beyond this threshold, the accumulator is clamped to prevent unbounded physics execution.
    /// </summary>
    private const int MaxPhysicsStepsPerFrame = 5;

    public int Priority => SystemPriorities.PhysicsSimulationSystem;

    /// <summary>
    /// Initializes the physics system.
    /// Resets the physics accumulator for clean state.
    /// </summary>
    public void OnInit()
    {
        // Reset physics accumulator for clean state
        _physicsAccumulator = 0f;
        EnsureBodiesCreated();
        Logger.Debug("PhysicsSimulationSystem initialized with priority {Priority}", Priority);
    }

    /// <summary>
    /// Updates the physics simulation using fixed timestep.
    /// Steps the physics world and synchronizes transforms with physics bodies.
    /// </summary>
    /// <param name="deltaTime">Variable frame time since last update.</param>
    public void OnUpdate(TimeSpan deltaTime)
    {
        // Fixed timestep physics simulation
        const int velocityIterations = 6;
        const int positionIterations = 2;
        var deltaSeconds = (float)deltaTime.TotalSeconds;

        // Accumulate time
        _physicsAccumulator += deltaSeconds;

        EnsureBodiesCreated();
        CleanupOrphanedBodies();

        // Step physics multiple times if needed to catch up
        var stepCount = 0;
        while (_physicsAccumulator >= CameraConfig.PhysicsTimestep && stepCount < MaxPhysicsStepsPerFrame)
        {
            SyncKinematicTransformsToBodies();
            SyncVelocitiesToBodies();
            physicsWorld.Step(CameraConfig.PhysicsTimestep, velocityIterations, positionIterations);
            _physicsAccumulator -= CameraConfig.PhysicsTimestep;
            stepCount++;
        }

        // If we hit max steps, clamp accumulator to prevent unbounded growth
        // while preserving some time debt for the next frame
        if (_physicsAccumulator >= CameraConfig.PhysicsTimestep)
        {
            _physicsAccumulator = CameraConfig.PhysicsTimestep * 0.5f; // Preserve half timestep
        }

        // Retrieve transform from Box2D and sync with entities
        var view = context.View<RigidBody2DComponent>();
        foreach (var (entity, component) in view)
        {
            var transform = entity.GetComponent<TransformComponent>();
            var collision = entity.GetComponent<BoxCollider2DComponent>();
            if (!bodyStore.TryGet(entity.Id, out var body))
                continue;

            var fixture = body.GetFixtureList();
            fixture.Density = collision.Density;
            fixture.m_friction = collision.Friction;
            fixture.Restitution = collision.Restitution;

            var position = body.GetPosition();
            transform.Translation = new Vector3(position.X, position.Y, 0);
            transform.Rotation = transform.Rotation with { Z = body.GetAngle() };

            if (component.BodyType is RigidBodyType.Dynamic or RigidBodyType.Kinematic)
            {
                var velocity = body.GetLinearVelocity();
                component.Velocity = velocity;
            }
        }
    }

    /// <summary>
    /// Shuts down the physics system.
    /// Called when the system is unregistered or scene is stopped.
    /// Properly destroys all Box2D bodies and clears component references.
    /// </summary>
    public void OnShutdown()
    {
        Logger.Debug("PhysicsSimulationSystem shutting down - cleaning up physics bodies");

        foreach (var (_, body) in bodyStore.Snapshot())
        {
            body.SetUserData(null);
            physicsWorld.DestroyBody(body);
        }

        bodyStore.Clear();
        Logger.Debug("PhysicsSimulationSystem shut down - all physics bodies destroyed");
    }

    private void EnsureBodiesCreated()
    {
        var view = context.View<RigidBody2DComponent>();
        foreach (var (entity, component) in view)
        {
            if (bodyStore.TryGet(entity.Id, out _))
                continue;

            var transform = entity.GetComponent<TransformComponent>();
            var bodyDef = new BodyDef
            {
                position = new Vector2(transform.Translation.X, transform.Translation.Y),
                angle = transform.Rotation.Z,
                type = RigidBody2DTypeToBox2DBody(component.BodyType),
                bullet = component.BodyType == RigidBodyType.Dynamic,
                gravityScale = component.GravityScale
            };

            var body = physicsWorld.CreateBody(bodyDef);
            body.SetFixedRotation(component.FixedRotation);
            body.SetUserData(entity);
            bodyStore.Set(entity.Id, body);

            if (!entity.HasComponent<BoxCollider2DComponent>())
                continue;

            var boxCollider = entity.GetComponent<BoxCollider2DComponent>();
            var shape = new PolygonShape();
            var actualSizeX = boxCollider.Size.X * transform.Scale.X;
            var actualSizeY = boxCollider.Size.Y * transform.Scale.Y;
            var actualOffsetX = boxCollider.Offset.X * transform.Scale.X;
            var actualOffsetY = boxCollider.Offset.Y * transform.Scale.Y;
            var center = new Vector2(actualOffsetX, actualOffsetY);
            shape.SetAsBox(actualSizeX, actualSizeY, center, 0.0f);

            var fixtureDef = new FixtureDef
            {
                shape = shape,
                density = boxCollider.Density,
                friction = boxCollider.Friction,
                restitution = boxCollider.Restitution,
                isSensor = boxCollider.IsTrigger
            };
            body.CreateFixture(fixtureDef);
        }
    }

    private void SyncVelocitiesToBodies()
    {
        foreach (var (entity, component) in context.View<RigidBody2DComponent>())
        {
            if (component.BodyType is RigidBodyType.Dynamic or RigidBodyType.Kinematic)
            {
                if (bodyStore.TryGet(entity.Id, out var body))
                {
                    body.SetLinearVelocity(component.Velocity);
                }
            }
        }
    }

    private void SyncKinematicTransformsToBodies()
    {
        foreach (var (entity, component) in context.View<RigidBody2DComponent>())
        {
            if (component.BodyType != RigidBodyType.Kinematic)
                continue;

            if (!bodyStore.TryGet(entity.Id, out var body))
                continue;

            var transform = entity.GetComponent<TransformComponent>();
            body.SetTransform(
                new Vector2(transform.Translation.X, transform.Translation.Y),
                transform.Rotation.Z);
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

            body.SetUserData(null);
            physicsWorld.DestroyBody(body);
            bodyStore.Remove(staleEntityId);
        }
    }

    private static BodyType RigidBody2DTypeToBox2DBody(RigidBodyType componentBodyType)
    {
        return componentBodyType switch
        {
            RigidBodyType.Static => BodyType.Static,
            RigidBodyType.Dynamic => BodyType.Dynamic,
            RigidBodyType.Kinematic => BodyType.Kinematic,
            _ => throw new ArgumentOutOfRangeException(nameof(componentBodyType), componentBodyType, null)
        };
    }

    /// <summary>
    /// Disposes the physics system and its associated Box2D World.
    /// Note: Box2D.NetStandard World doesn't implement IDisposable, but we call this
    /// to maintain consistent disposal patterns. The World will be garbage collected.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        // Box2D.NetStandard World doesn't implement IDisposable
        // Bodies should already be destroyed by OnShutdown() before disposal
        // The World will be garbage collected

        _disposed = true;
        GC.SuppressFinalize(this);
        Logger.Debug("PhysicsSimulationSystem disposed");
    }
}
