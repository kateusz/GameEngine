using System.Numerics;
using Box2D.NetStandard.Dynamics.World;
using ECS;
using Engine.Renderer.Cameras;
using Engine.Scene.Components;
using Serilog;

namespace Engine.Scene.Systems;

/// <summary>
/// System responsible for physics simulation using Box2D.
/// Handles fixed timestep physics stepping and synchronization between physics bodies and transforms.
/// </summary>
public class PhysicsSimulationSystem : ISystem
{
    private static readonly ILogger Logger = Log.ForContext<PhysicsSimulationSystem>();

    private readonly World _physicsWorld;

    // Fixed timestep accumulator for deterministic physics
    private float _physicsAccumulator = 0f;

    /// <summary>
    /// Maximum physics steps per frame to prevent spiral of death.
    /// At 60Hz physics with 16ms frames, this allows catching up from frame spikes up to ~83ms.
    /// Beyond this threshold, the accumulator is clamped to prevent unbounded physics execution.
    /// </summary>
    private const int MaxPhysicsStepsPerFrame = 5;

    /// <summary>
    /// Gets the priority of this system.
    /// Priority 100 ensures physics runs after transforms (10-20) and before rendering (200+).
    /// </summary>
    public int Priority => 100;

    /// <summary>
    /// Creates a new PhysicsSimulationSystem with the specified physics world.
    /// </summary>
    /// <param name="physicsWorld">The Box2D World instance to simulate.</param>
    public PhysicsSimulationSystem(World physicsWorld)
    {
        _physicsWorld = physicsWorld ?? throw new ArgumentNullException(nameof(physicsWorld));
    }

    /// <summary>
    /// Initializes the physics system.
    /// Resets the physics accumulator for clean state.
    /// </summary>
    public void OnInit()
    {
        // Reset physics accumulator for clean state
        _physicsAccumulator = 0f;
        Logger.Debug("PhysicsSimulationSystem initialized");
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
        int stepCount = 0;
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
        var view = Context.Instance.View<RigidBody2DComponent>();
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
    /// Currently no cleanup needed as World is managed by Scene.
    /// </summary>
    public void OnShutdown()
    {
        Logger.Debug("PhysicsSimulationSystem shut down");
    }
}
