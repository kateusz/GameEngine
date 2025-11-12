using System.Numerics;
using Box2D.NetStandard.Collision.Shapes;
using Box2D.NetStandard.Dynamics.Bodies;
using Box2D.NetStandard.Dynamics.Fixtures;
using Box2D.NetStandard.Dynamics.World;
using ECS;
using Engine.Scene.Components;
using Serilog;

namespace Engine.Scene.Systems;

/// <summary>
/// System responsible for initializing physics bodies when runtime starts.
/// Extracts physics initialization logic from Scene.OnRuntimeStart() to maintain ECS separation of concerns.
/// This is a PER-SCENE system - each scene has its own instance with its own physics world.
/// </summary>
public class PhysicsInitializationSystem : ISystem
{
    private static readonly ILogger Logger = Log.ForContext<PhysicsInitializationSystem>();

    private readonly World _physicsWorld;
    private readonly IContext _context;

    /// <summary>
    /// Gets the priority of this system.
    /// Priority 50 ensures physics initialization runs before PhysicsSimulationSystem (100).
    /// </summary>
    public int Priority => 50;

    /// <summary>
    /// Creates a new PhysicsInitializationSystem with the specified physics world.
    /// </summary>
    /// <param name="physicsWorld">The Box2D World instance to create bodies in.</param>
    /// <param name="context">The ECS context for querying entities.</param>
    public PhysicsInitializationSystem(World physicsWorld, IContext context)
    {
        _physicsWorld = physicsWorld ?? throw new ArgumentNullException(nameof(physicsWorld));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Initializes all physics bodies for entities with RigidBody2DComponent.
    /// Called once when the scene runtime starts.
    /// </summary>
    public void OnInit()
    {
        Logger.Debug("PhysicsInitializationSystem initializing physics bodies");

        var view = _context.View<RigidBody2DComponent>();
        foreach (var (entity, component) in view)
        {
            InitializePhysicsBody(entity, component);
        }

        Logger.Debug("PhysicsInitializationSystem initialized with priority {Priority}", Priority);
    }

    /// <summary>
    /// Initializes a physics body for the given entity.
    /// </summary>
    /// <param name="entity">The entity to initialize physics for.</param>
    /// <param name="component">The RigidBody2DComponent containing physics configuration.</param>
    private void InitializePhysicsBody(Entity entity, RigidBody2DComponent component)
    {
        var transform = entity.GetComponent<TransformComponent>();
        var bodyDef = new BodyDef
        {
            position = new Vector2(transform.Translation.X, transform.Translation.Y),
            angle = transform.Rotation.Z,
            type = RigidBody2DTypeToBox2DBody(component.BodyType),
            bullet = component.BodyType == RigidBodyType.Dynamic
        };

        var body = _physicsWorld.CreateBody(bodyDef);
        body.SetFixedRotation(component.FixedRotation);
        body.SetUserData(entity);
        component.RuntimeBody = body;

        if (entity.HasComponent<BoxCollider2DComponent>())
        {
            var boxCollider = entity.GetComponent<BoxCollider2DComponent>();
            CreateBoxFixture(body, boxCollider, transform);
        }
    }

    /// <summary>
    /// Creates a box fixture for the given physics body.
    /// </summary>
    /// <param name="body">The Box2D body to attach the fixture to.</param>
    /// <param name="collider">The BoxCollider2DComponent with collider configuration.</param>
    /// <param name="transform">The TransformComponent for calculating actual size and offset.</param>
    private void CreateBoxFixture(Body body, BoxCollider2DComponent collider, TransformComponent transform)
    {
        var shape = new PolygonShape();

        var actualSizeX = collider.Size.X * transform.Scale.X;
        var actualSizeY = collider.Size.Y * transform.Scale.Y;
        var actualOffsetX = collider.Offset.X * transform.Scale.X;
        var actualOffsetY = collider.Offset.Y * transform.Scale.Y;

        var center = new Vector2(actualOffsetX, actualOffsetY);
        shape.SetAsBox(actualSizeX / 2.0f, actualSizeY / 2.0f, center, 0.0f);

        var fixtureDef = new FixtureDef
        {
            shape = shape,
            density = collider.Density,
            friction = collider.Friction,
            restitution = collider.Restitution,
            isSensor = collider.IsTrigger
        };

        body.CreateFixture(fixtureDef);
    }

    /// <summary>
    /// Converts engine RigidBodyType to Box2D BodyType.
    /// </summary>
    /// <param name="type">The engine's RigidBodyType.</param>
    /// <returns>The corresponding Box2D BodyType.</returns>
    private BodyType RigidBody2DTypeToBox2DBody(RigidBodyType type)
    {
        return type switch
        {
            RigidBodyType.Static => BodyType.Static,
            RigidBodyType.Dynamic => BodyType.Dynamic,
            RigidBodyType.Kinematic => BodyType.Kinematic,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    /// <summary>
    /// No-op update - physics initialization happens once in OnInit().
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update.</param>
    public void OnUpdate(TimeSpan deltaTime)
    {
        // Physics bodies are initialized once in OnInit(), no per-frame update needed
    }

    /// <summary>
    /// No-op shutdown - physics body cleanup is handled by PhysicsSimulationSystem.OnShutdown().
    /// </summary>
    public void OnShutdown()
    {
        // Physics body cleanup is handled by PhysicsSimulationSystem.OnShutdown()
        // to avoid double-cleanup issues
        Logger.Debug("PhysicsInitializationSystem shut down");
    }
}
