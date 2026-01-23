using System.Numerics;
using ECS;
using ECS.Systems;
using Engine.Renderer;
using Engine.Renderer.Cameras;
using Engine.Scene.Components;
using Serilog;

namespace Engine.Scene.Systems;

/// <summary>
/// System responsible for rendering 3D models
/// </summary>
internal sealed class ModelRenderingSystem(IGraphics3D graphics3D, IContext context) : ISystem
{
    private static readonly ILogger Logger = Log.ForContext<ModelRenderingSystem>();
    
    public int Priority => SystemPriorities.ModelRenderSystem;
    
    public void OnInit()
    {
        Logger.Debug("ModelRenderingSystem initialized with priority {Priority}", Priority);
    }

    /// <summary>
    /// Called every frame to update and render 3D models.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update.</param>
    public void OnUpdate(TimeSpan deltaTime)
    {
        Camera? mainCamera = null;
        var cameraTransform = Matrix4x4.Identity;

        var cameraGroup = context.View<CameraComponent>();
        foreach (var (entity, cameraComponent) in cameraGroup)
        {
            if (cameraComponent.Primary)
            {
                mainCamera = cameraComponent.Camera;
                var transformComponent = entity.GetComponent<TransformComponent>();
                cameraTransform = transformComponent.GetTransform();
                break;
            }
        }
        
        if (mainCamera == null)
            return;

        graphics3D.BeginScene(mainCamera, cameraTransform);
        
        var view = context.View<MeshComponent>();
        foreach (var (entity, meshComponent) in view)
        {
            var transformComponent = entity.GetComponent<TransformComponent>();
            var modelRendererComponent = entity.GetComponent<ModelRendererComponent>();

            graphics3D.DrawModel(
                transformComponent.GetTransform(),
                meshComponent,
                modelRendererComponent,
                entity.Id
            );
        }

        graphics3D.EndScene();
    }
    
    public void OnShutdown() {}
}
