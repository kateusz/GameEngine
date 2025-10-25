using System.Numerics;
using ECS;
using Engine.Renderer;
using Engine.Renderer.Cameras;
using Engine.Renderer.Textures;
using Engine.Scene.Components;

namespace Engine.Scene.Systems;

/// <summary>
/// System responsible for rendering 2D subtextures (sprite atlas/sprite sheet regions).
/// Operates on entities with SubTextureRendererComponent and TransformComponent.
/// </summary>
/// <remarks>
/// NOTE: SubTextureRendererComponent is currently incomplete - it only stores coords and texture,
/// but not cellSize or spriteSize. This system uses default values:
/// - cellSize: 16x16 pixels
/// - spriteSize: 1x1 cells
///
/// TODO: Enhance SubTextureRendererComponent to include cellSize and spriteSize properties.
/// </remarks>
public class SubTextureRenderingSystem : ISystem
{
    private readonly IGraphics2D _renderer;

    // Default cell size for sprite sheets (in pixels)
    private readonly Vector2 _defaultCellSize = new(16, 16);

    // Default sprite size (in cells)
    private readonly Vector2 _defaultSpriteSize = new(1, 1);

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
        // No initialization required
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

            // Create SubTexture2D from component data using default cell/sprite sizes
            // TODO: Once SubTextureRendererComponent is enhanced with cellSize/spriteSize,
            // use those values instead of defaults
            var subTexture = SubTexture2D.CreateFromCoords(
                subtextureComponent.Texture,
                subtextureComponent.Coords,
                _defaultCellSize,
                _defaultSpriteSize
            );

            // Get transform matrix and extract position/rotation
            var transform = transformComponent.GetTransform();
            var position = transformComponent.Translation;
            var rotation = transformComponent.Rotation.Z; // 2D rotation around Z axis

            // Use the scale to determine quad size (defaults to cell size if scale is 1)
            var size = _defaultCellSize * new Vector2(transformComponent.Scale.X, transformComponent.Scale.Y);

            // Draw the subtexture quad
            _renderer.DrawQuad(position, size, rotation, subTexture);
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
