using System.Numerics;
using Box2D.NetStandard.Dynamics.World;
using ECS;
using ECS.Systems;
using Engine.Renderer.Cameras;
using Engine.Scene.Components;
using Serilog;

namespace Engine.Scene.Systems;

/// <summary>
/// System responsible for physics simulation using Box2D.
/// Handles fixed timestep physics stepping and synchronization between physics bodies and transforms.
/// This is a PER-SCENE system - each scene has its own instance with its own physics world.
/// </summary>
internal sealed class PhysicsSimulationSystem : ISystem, IDisposable
{
    private static readonly ILogger Logger = Log.ForContext<PhysicsSimulationSystem>();

    private readonly World _physicsWorld;
    private readonly IContext _context;

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
    /// Creates a new PhysicsSimulationSystem with the specified physics world.
    /// </summary>
    /// <param name="physicsWorld">The Box2D World instance to simulate.</param>
    /// <param name="context">The ECS context for querying entities.</param>
    public PhysicsSimulationSystem(World physicsWorld, IContext context)
    {
        _physicsWorld = physicsWorld ?? throw new ArgumentNullException(nameof(physicsWorld));
        _context = context;
    }

    /// <summary>
    /// Initializes the physics system.
    /// Resets the physics accumulator for clean state.
    /// </summary>
    public void OnInit()
    {
        // Reset physics accumulator for clean state
        _physicsAccumulator = 0f;
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

        // Step physics multiple times if needed to catch up
        var stepCount = 0;
        while (_physicsAccumulator >= CameraConfig.PhysicsTimestep && stepCount < MaxPhysicsStepsPerFrame)
        {
            _physicsWorld.Step(CameraConfig.PhysicsTimestep, velocityIterations, positionIterations);
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
        var view = _context.View<RigidBody2DComponent>();
        foreach (var (entity, component) in view)
        {
            var transform = entity.GetComponent<TransformComponent>();
            var collision = entity.GetComponent<BoxCollider2DComponent>();
            var body = component.RuntimeBody;

            if (body != null)
            {
                var fixture = body.GetFixtureList();
                fixture.Density = collision.Density;
                fixture.m_friction = collision.Friction;
                fixture.Restitution = collision.Restitution;

                var position = body.GetPosition();
                transform.Translation = new Vector3(position.X, position.Y, 0);
                transform.Rotation = transform.Rotation with { Z = body.GetAngle() };
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

        // Properly destroy all physics bodies before clearing references
        var view = _context.View<RigidBody2DComponent>();
        foreach (var (entity, component) in view)
        {
            if (component.RuntimeBody != null)
            {
                // Clear user data to prevent dangling references
                component.RuntimeBody.SetUserData(null);

                // Destroy the Box2D body
                _physicsWorld.DestroyBody(component.RuntimeBody);

                // Clear component reference to prevent double-cleanup
                component.RuntimeBody = null;
            }
        }

        Logger.Debug("PhysicsSimulationSystem shut down - all physics bodies destroyed");
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
        Logger.Debug("PhysicsSimulationSystem disposed");
    }
}
