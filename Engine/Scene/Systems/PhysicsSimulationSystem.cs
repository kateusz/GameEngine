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
/// This is a PER-SCENE system - each scene has its own instance with its own physics world.
/// </summary>
public class PhysicsSimulationSystem : ISystem, IDisposable
{
    private static readonly ILogger Logger = Log.ForContext<PhysicsSimulationSystem>();

    private readonly World _physicsWorld;
    private readonly SceneContactListener _contactListener;
    private readonly ScenePhysicsSettings _settings;

    // Fixed timestep accumulator for deterministic physics
    private float _physicsAccumulator = 0f;
    private bool _disposed = false;

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
    /// Creates a new PhysicsSimulationSystem with the specified physics world, contact listener, and settings.
    /// </summary>
    /// <param name="physicsWorld">The Box2D World instance to simulate.</param>
    /// <param name="contactListener">The contact listener for collision events.</param>
    /// <param name="settings">Physics simulation settings (gravity, iterations, etc.).</param>
    public PhysicsSimulationSystem(World physicsWorld, SceneContactListener contactListener, ScenePhysicsSettings settings)
    {
        _physicsWorld = physicsWorld ?? throw new ArgumentNullException(nameof(physicsWorld));
        _contactListener = contactListener ?? throw new ArgumentNullException(nameof(contactListener));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
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
    /// Uses interpolation for smooth rendering at non-60fps frame rates.
    /// </summary>
    /// <param name="deltaTime">Variable frame time since last update.</param>
    public void OnUpdate(TimeSpan deltaTime)
    {
        // Use configurable solver iterations from settings
        var velocityIterations = _settings.VelocityIterations;
        var positionIterations = _settings.PositionIterations;
        var deltaSeconds = (float)deltaTime.TotalSeconds * _settings.TimeScale;

        // Accumulate time
        _physicsAccumulator += deltaSeconds;

        // Step physics multiple times if needed to catch up
        int stepCount = 0;
        while (_physicsAccumulator >= CameraConfig.PhysicsTimestep && stepCount < MaxPhysicsStepsPerFrame)
        {
            // Store previous transforms before stepping for interpolation
            StorePreviousTransforms();
            
            _physicsWorld.Step(CameraConfig.PhysicsTimestep, velocityIterations, positionIterations);
            _physicsAccumulator -= CameraConfig.PhysicsTimestep;
            stepCount++;
        }

        // Process collision events after all physics steps complete
        // This ensures thread safety and enables future parallelization
        _contactListener.ProcessContactEvents();

        // If we hit max steps, clamp accumulator to prevent unbounded growth
        // while preserving some time debt for the next frame
        if (_physicsAccumulator >= CameraConfig.PhysicsTimestep)
        {
            _physicsAccumulator = CameraConfig.PhysicsTimestep * 0.5f; // Preserve half timestep
        }

        // Calculate interpolation alpha (0-1) based on accumulator remainder
        float alpha = _physicsAccumulator / CameraConfig.PhysicsTimestep;

        // Update fixture properties (only when dirty) and interpolate transforms
        UpdateAndInterpolateTransforms(alpha);
    }

    /// <summary>
    /// Stores the current physics body positions and angles before stepping.
    /// This is required for smooth interpolation between physics steps.
    /// </summary>
    private void StorePreviousTransforms()
    {
        var view = Context.Instance.View<RigidBody2DComponent>();
        foreach (var (entity, component) in view)
        {
            var body = component.RuntimeBody;
            if (body != null)
            {
                component.PreviousPosition = body.GetPosition();
                component.PreviousAngle = body.GetAngle();
            }
        }
    }

    /// <summary>
    /// Updates fixture properties (when dirty) and interpolates transforms for smooth rendering.
    /// Only updates fixture properties when they have been modified (IsDirty flag).
    /// </summary>
    /// <param name="alpha">Interpolation factor (0-1) between previous and current physics state.</param>
    private void UpdateAndInterpolateTransforms(float alpha)
    {
        var view = Context.Instance.View<RigidBody2DComponent>();
        foreach (var (entity, component) in view)
        {
            // Validate required components
            if (!entity.HasComponent<TransformComponent>())
            {
                Logger.Warning("Entity {EntityName} (ID: {EntityId}) has RigidBody2D but missing TransformComponent", 
                    entity.Name, entity.Id);
                continue;
            }

            var transform = entity.GetComponent<TransformComponent>();
            var body = component.RuntimeBody;

            if (body != null)
            {
                // Only update fixture properties if they've been modified
                if (entity.HasComponent<BoxCollider2DComponent>())
                {
                    var collision = entity.GetComponent<BoxCollider2DComponent>();
                    
                    if (collision.IsDirty)
                    {
                        var fixture = body.GetFixtureList();
                        fixture.Density = collision.Density;
                        fixture.m_friction = collision.Friction;
                        fixture.Restitution = collision.Restitution;
                        
                        // Required after density change to recalculate mass
                        body.ResetMassData();
                        
                        collision.ClearDirtyFlag();
                    }
                }

                // Interpolate position and rotation for smooth rendering
                var currentPos = body.GetPosition();
                var previousPos = component.PreviousPosition;
                
                // Linear interpolation of position
                var interpolatedX = previousPos.X + (currentPos.X - previousPos.X) * alpha;
                var interpolatedY = previousPos.Y + (currentPos.Y - previousPos.Y) * alpha;
                transform.Translation = new Vector3(interpolatedX, interpolatedY, 0);

                // Angular interpolation (handles wrapping)
                var currentAngle = body.GetAngle();
                var previousAngle = component.PreviousAngle;
                var angleDiff = currentAngle - previousAngle;
                
                // Normalize angle difference to [-π, π]
                while (angleDiff > MathF.PI) angleDiff -= MathF.Tau;
                while (angleDiff < -MathF.PI) angleDiff += MathF.Tau;
                
                var interpolatedAngle = previousAngle + angleDiff * alpha;
                transform.Rotation = transform.Rotation with { Z = interpolatedAngle };
            }
        }
    }

    /// <summary>
    /// Shuts down the physics system.
    /// Called when the system is unregistered or scene is stopped.
    /// </summary>
    public void OnShutdown()
    {
        Logger.Debug("PhysicsSimulationSystem shut down");
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
        // Bodies should already be destroyed by Scene.OnRuntimeStop() before disposal
        // The World will be garbage collected

        _disposed = true;
        Logger.Debug("PhysicsSimulationSystem disposed");
    }
}
