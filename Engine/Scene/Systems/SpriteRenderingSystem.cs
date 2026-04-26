using ECS;
using ECS.Systems;
using Engine.Renderer;
using Engine.Scene;
using Engine.Scene.Components;
using Serilog;

namespace Engine.Scene.Systems;

/// <summary>
/// System responsible for rendering 2D sprites.
/// </summary>
internal sealed class SpriteRenderingSystem(IGraphics2D renderer, IContext context, IPrimaryCameraProvider cameraProvider) : ISystem
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
        if (cameraProvider.Camera == null)
            return;

        renderer.BeginScene(cameraProvider.Camera, cameraProvider.Transform);

        var spriteGroup = context.View<SpriteRendererComponent>();
        foreach (var (entity, spriteRendererComponent) in spriteGroup)
        {
            renderer.DrawSprite(entity.GetWorldTransform(context), spriteRendererComponent, entity.Id);
        }
        
        renderer.EndScene();
    }
    
    public void OnShutdown() {}
}
