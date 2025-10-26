using System.Numerics;
using ECS;
using Engine.Math;
using Engine.Renderer;
using Engine.Renderer.Cameras;
using Engine.Renderer.Textures;
using Engine.Scene.Components;
using Serilog;

namespace Engine.Scene.Systems;

/// <summary>
/// System responsible for rendering 2D subtextures (sprite atlas/sprite sheet regions).
/// Operates on entities with SubTextureRendererComponent and TransformComponent.
/// </summary>
/// <remarks>
/// Cell size and sprite size are configured per-entity via SubTextureRendererComponent properties.
/// This allows for flexible sprite atlas configurations with different cell sizes.
/// </remarks>
public class SubTextureRenderingSystem : ISystem
{
    private static readonly ILogger Logger = Log.ForContext<SubTextureRenderingSystem>();

    private readonly IGraphics2D _renderer;

    /// <summary>
    /// Priority of 205 ensures this system renders after regular sprites (200)
    /// but before 3D models (210).
    /// </summary>
    public int Priority => 205;

    /// <summary>
    /// Creates a new SubTextureRenderingSystem.
    /// </summary>
    /// <param name="renderer">The 2D renderer interface to use for drawing subtextures.</param>
    public SubTextureRenderingSystem(IGraphics2D renderer)
    {
        _renderer = renderer;
    }

    /// <summary>
    /// Initializes the subtexture rendering system.
    /// </summary>
    public void OnInit()
    {
        Logger.Debug("SubTextureRenderingSystem initialized with priority {Priority}", Priority);
    }

    /// <summary>
    /// Updates and renders all subtextures in the scene.
    /// Finds the primary camera and renders all entities with subtexture components.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last frame.</param>
    public void OnUpdate(TimeSpan deltaTime)
    {
        // Find the primary camera
        Camera? mainCamera = null;
        var cameraGroup = Context.Instance.GetGroup([typeof(TransformComponent), typeof(CameraComponent)]);
        var cameraTransform = Matrix4x4.Identity;

        foreach (var entity in cameraGroup)
        {
            var transformComponent = entity.GetComponent<TransformComponent>();
            var cameraComponent = entity.GetComponent<CameraComponent>();

            if (cameraComponent.Primary)
            {
                mainCamera = cameraComponent.Camera;
                cameraTransform = transformComponent.GetTransform();
                break;
            }
        }

        // Only render if we have a camera
        if (mainCamera == null)
            return;

        // Begin rendering with the camera's view and projection
        _renderer.BeginScene(mainCamera, cameraTransform);

        // Render all subtextures
        var subtextureGroup = Context.Instance.GetGroup([typeof(TransformComponent), typeof(SubTextureRendererComponent)]);
        foreach (var entity in subtextureGroup)
        {
            var subtextureComponent = entity.GetComponent<SubTextureRendererComponent>();
            var transformComponent = entity.GetComponent<TransformComponent>();

            // Skip if no texture is assigned
            if (subtextureComponent.Texture == null)
                continue;

            // Create SubTexture2D from component data using component's cell/sprite sizes
            var subTexture = SubTexture2D.CreateFromCoords(
                subtextureComponent.Texture,
                subtextureComponent.Coords,
                subtextureComponent.CellSize,
                subtextureComponent.SpriteSize
            );

            // Extract position and rotation from transform component
            var position = transformComponent.Translation;
            var rotation = transformComponent.Rotation.Z; // 2D rotation around Z axis

            // Compute size from scale and cell size
            var size = subtextureComponent.CellSize * new Vector2(transformComponent.Scale.X, transformComponent.Scale.Y);

            // Build transform matrix: Translation * Rotation * Scale
            var transform = Matrix4x4.CreateTranslation(position);
            if (System.Math.Abs(rotation) > float.Epsilon)
            {
                transform *= Matrix4x4.CreateRotationZ(MathHelpers.DegreesToRadians(rotation));
            }
            transform *= Matrix4x4.CreateScale(size.X, size.Y, 1.0f);

            // Draw the subtexture quad with entity ID for picking
            _renderer.DrawQuad(transform, subTexture.Texture, subTexture.TexCoords, 1.0f, Vector4.One, entity.Id);
        }

        // End the rendering batch
        _renderer.EndScene();
    }

    /// <summary>
    /// Cleans up the subtexture rendering system.
    /// </summary>
    public void OnShutdown()
    {
        // No cleanup required
    }
}
