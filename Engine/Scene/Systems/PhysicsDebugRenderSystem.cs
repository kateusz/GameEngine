using System.Numerics;
using Box2D.NetStandard.Dynamics.Bodies;
using ECS;
using Engine.Core;
using Engine.Renderer;
using Engine.Scene.Components;
using Serilog;

namespace Engine.Scene.Systems;

/// <summary>
/// System responsible for rendering physics debug visualizations.
/// Draws wireframe overlays for collision shapes to aid in debugging physics behavior.
/// </summary>
public class PhysicsDebugRenderSystem : ISystem
{
    private static readonly ILogger Logger = Log.ForContext<PhysicsDebugRenderSystem>();

    private readonly IGraphics2D _renderer;

    /// <summary>
    /// Execution priority for this system.
    /// Set to 500 to ensure it renders after main rendering systems.
    /// </summary>
    public int Priority => 500;

    /// <summary>
    /// Creates a new PhysicsDebugRenderSystem.
    /// </summary>
    /// <param name="renderer">The 2D renderer interface to use for drawing debug shapes.</param>
    public PhysicsDebugRenderSystem(IGraphics2D renderer)
    {
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
    }

    /// <summary>
    /// System initialization
    /// </summary>
    public void OnInit()
    {
        Logger.Debug("PhysicsDebugRenderSystem initialized with priority {Priority}", Priority);
    }

    /// <summary>
    /// Updates the system, rendering debug visualizations for all physics bodies.
    /// Only renders if debug settings are enabled.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last update (unused by this system).</param>
    public void OnUpdate(TimeSpan deltaTime)
    {
        // Only draw collider bounds if the flag is enabled
        if (!DebugSettings.Instance.ShowColliderBounds)
            return;

        // Find the primary camera for rendering
        var cameraGroup = Context.Instance.GetGroup([typeof(TransformComponent), typeof(CameraComponent)]);

        foreach (var entity in cameraGroup)
        {
            var cameraComponent = entity.GetComponent<CameraComponent>();
            if (cameraComponent.Primary)
            {
                var transformComponent = entity.GetComponent<TransformComponent>();
                var cameraTransform = transformComponent.GetTransform();

                // Begin rendering with the camera's view and projection
                _renderer.BeginScene(cameraComponent.Camera, cameraTransform);

                DrawPhysicsDebug();

                // End the rendering batch
                _renderer.EndScene();
                break;
            }
        }
    }

    /// <summary>
    /// System shutdown.
    /// </summary>
    public void OnShutdown()
    {
        // No cleanup required
    }

    /// <summary>
    /// Renders debug wireframes for all entities with physics bodies.
    /// Color coding is based on body type and state:
    /// - Static bodies: Green
    /// - Kinematic bodies: Blue
    /// - Dynamic bodies (awake): Pink/Red
    /// - Dynamic bodies (sleeping): Gray
    /// - Disabled bodies: Tan
    /// </summary>
    private void DrawPhysicsDebug()
    {
        var rigidBodyView = Context.Instance.View<RigidBody2DComponent>();
        foreach (var (entity, rigidBodyComponent) in rigidBodyView)
        {
            if (rigidBodyComponent.RuntimeBody == null)
                continue;

            // Get pose from Box2D body
            var body = rigidBodyComponent.RuntimeBody;
            var bodyPosition = body.GetPosition();
            var angle = body.GetAngle();


            // Draw BoxCollider2D if it exists
            if (entity.TryGetComponent<BoxCollider2DComponent>(out var boxCollider))
            {
                var transform = entity.GetComponent<TransformComponent>();
                var color = GetBodyDebugColor(rigidBodyComponent.RuntimeBody);

                // Full size (Box2D uses half-extents)
                var size = new Vector2(
                    boxCollider.Size.X * 2.0f * transform.Scale.X,
                    boxCollider.Size.Y * 2.0f * transform.Scale.Y
                );

                // Apply collider offset in body space, then rotate into world
                var offset = new Vector2(
                    boxCollider.Offset.X * transform.Scale.X,
                    boxCollider.Offset.Y * transform.Scale.Y
                );
                var cos = MathF.Cos(angle);
                var sin = MathF.Sin(angle);
                var rotatedOffset = new Vector2(
                    offset.X * cos - offset.Y * sin,
                    offset.X * sin + offset.Y * cos
                );
                var worldPos = new Vector3(bodyPosition.X + rotatedOffset.X,
                    bodyPosition.Y + rotatedOffset.Y, 0.0f);

                // Build TRS (match engine’s order)
                var trs = Matrix4x4.CreateTranslation(worldPos)
                          * Matrix4x4.CreateRotationZ(angle)
                          * Matrix4x4.CreateScale(size.X, size.Y, 1.0f);

                _renderer.DrawRect(trs, color, entity.Id);
            }
        }
    }

    /// <summary>
    /// Determines the debug color for a physics body based on its type and state.
    /// </summary>
    /// <param name="body">The Box2D body to get the color for.</param>
    /// <returns>A color vector representing the body's state.</returns>
    private static Vector4 GetBodyDebugColor(Body body)
    {
        if (!body.IsEnabled())
            return new Vector4(0.5f, 0.5f, 0.3f, 1.0f); // Inactive (tan)

        return body.Type() switch
        {
            BodyType.Static => new Vector4(0.5f, 0.9f, 0.5f, 1.0f), // Green
            BodyType.Kinematic => new Vector4(0.5f, 0.5f, 0.9f, 1.0f), // Blue
            _ => body.IsAwake()
                ? new Vector4(0.9f, 0.7f, 0.7f, 1.0f) // Pink (active dynamic)
                : new Vector4(0.6f, 0.6f, 0.6f, 1.0f) // Gray (sleeping dynamic)
        };
    }
}