using System.Numerics;
using ECS;
using ECS.Systems;
using Engine.Renderer;
using Engine.Renderer.Cameras;
using Engine.Scene.Components;
using Serilog;

namespace Engine.Scene.Systems;

/// <summary>
/// System responsible for rendering 2D sprites.
/// </summary>
internal sealed class SpriteRenderingSystem(IGraphics2D renderer, IContext context) : ISystem
{
    private static readonly ILogger Logger = Log.ForContext<SpriteRenderingSystem>();

    public int Priority => SystemPriorities.SpriteRenderSystem;
    
    public void OnInit()
    {
        Logger.Debug("SpriteRenderingSystem initialized with priority {Priority}", Priority);
    }

    /// <summary>
    /// Updates and renders all sprites in the scene.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last frame.</param>
    public void OnUpdate(TimeSpan deltaTime)
    {
        // Find the primary camera
        Camera? mainCamera = null;
        var cameraGroup = context.View<CameraComponent>();
        var cameraTransform = Matrix4x4.Identity;

        foreach (var (entity, cameraComponent) in cameraGroup)
        {
            var transformComponent = entity.GetComponent<TransformComponent>();
            if (cameraComponent.Primary)
            {
                mainCamera = cameraComponent.Camera;
                cameraTransform = transformComponent.GetTransform();
                break;
            }
        }
        
        if (mainCamera == null)
            return;
        
        renderer.BeginScene(mainCamera, cameraTransform);

        var spriteGroup = context.View<SpriteRendererComponent>();
        foreach (var (entity, spriteRendererComponent) in spriteGroup)
        {
            var transformComponent = entity.GetComponent<TransformComponent>();
            renderer.DrawSprite(transformComponent.GetTransform(), spriteRendererComponent, entity.Id);
        }
        
        renderer.EndScene();
    }
    
    public void OnShutdown() {}
}
