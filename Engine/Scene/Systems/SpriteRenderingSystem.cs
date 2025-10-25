using System.Numerics;
using ECS;
using Engine.Renderer;
using Engine.Renderer.Cameras;
using Engine.Scene.Components;

namespace Engine.Scene.Systems;

/// <summary>
/// System responsible for rendering 2D sprites.
/// Operates on entities with SpriteRendererComponent and TransformComponent.
/// </summary>
public class SpriteRenderingSystem : ISystem
{
    private readonly IGraphics2D _renderer;

    /// <summary>
    /// Priority of 200 ensures this system renders after scripts (priority 150).
    /// </summary>
    public int Priority => 200;

    /// <summary>
    /// Creates a new SpriteRenderingSystem.
    /// </summary>
    /// <param name="renderer">The 2D renderer interface to use for drawing sprites.</param>
    public SpriteRenderingSystem(IGraphics2D renderer)
    {
        _renderer = renderer;
    }

    /// <summary>
    /// Initializes the sprite rendering system.
    /// </summary>
    public void OnInit()
    {
        // No initialization required
    }

    /// <summary>
    /// Updates and renders all sprites in the scene.
    /// Finds the primary camera and renders all entities with sprite components.
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

        // Render all sprites
        var spriteGroup = Context.Instance.GetGroup([typeof(TransformComponent), typeof(SpriteRendererComponent)]);
        foreach (var entity in spriteGroup)
        {
            var spriteRendererComponent = entity.GetComponent<SpriteRendererComponent>();
            var transformComponent = entity.GetComponent<TransformComponent>();
            _renderer.DrawSprite(transformComponent.GetTransform(), spriteRendererComponent, entity.Id);
        }

        // End the rendering batch
        _renderer.EndScene();
    }

    /// <summary>
    /// Cleans up the sprite rendering system.
    /// </summary>
    public void OnShutdown()
    {
        // No cleanup required
    }
}
