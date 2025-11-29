using System.Numerics;
using ECS;
using ECS.Systems;
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
internal sealed class SubTextureRenderingSystem : ISystem
{
    private static readonly ILogger Logger = Log.ForContext<SubTextureRenderingSystem>();

    private readonly IGraphics2D _renderer;
    private readonly IContext _context;

    /// <summary>
    /// Priority of 205 ensures this system renders after regular sprites (200)
    /// but before 3D models (210).
    /// </summary>
    public int Priority => 205;

    /// <summary>
    /// Creates a new SubTextureRenderingSystem.
    /// </summary>
    /// <param name="renderer">The 2D renderer interface to use for drawing subtextures.</param>
    /// <param name="context">The ECS context for querying entities.</param>
    public SubTextureRenderingSystem(IGraphics2D renderer, IContext context)
    {
        _renderer = renderer;
        _context = context;
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
        var cameraGroup = _context.GetGroup([typeof(TransformComponent), typeof(CameraComponent)]);
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
        var subtextureGroup =
            _context.GetGroup([typeof(TransformComponent), typeof(SubTextureRendererComponent)]);
        foreach (var entity in subtextureGroup)
        {
            var subtextureComponent = entity.GetComponent<SubTextureRendererComponent>();
            var transformComponent = entity.GetComponent<TransformComponent>();

            // Skip if no texture is assigned
            if (subtextureComponent.Texture == null)
                continue;

            // Use the transform component's matrix directly (same as SpriteRenderingSystem)
            // The transform scale determines the world-space size of the rendered sprite
            var transform = transformComponent.GetTransform();

            // Use pre-calculated TexCoords if available (e.g., from animation system)
            // Otherwise calculate from grid coordinates
            Vector2[] texCoords;
            if (subtextureComponent.TexCoords != null)
            {
                // Direct UV coordinates (used by animation system)
                texCoords = subtextureComponent.TexCoords;
            }
            else
            {
                // Calculate from grid coordinates (traditional subtexture rendering)
                var subTexture = SubTexture2D.CreateFromCoords(
                    subtextureComponent.Texture,
                    subtextureComponent.Coords,
                    subtextureComponent.CellSize,
                    subtextureComponent.SpriteSize
                );
                texCoords = subTexture.TexCoords;
            }
            // Draw the subtexture quad with entity ID for picking
            _renderer.DrawQuad(transform, subtextureComponent.Texture, texCoords, 1.0f, Vector4.One, entity.Id);
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