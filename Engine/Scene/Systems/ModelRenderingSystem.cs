using System.Numerics;
using ECS;
using Engine.Renderer;
using Engine.Renderer.Cameras;
using Engine.Scene.Components;

namespace Engine.Scene.Systems;

/// <summary>
/// System responsible for rendering 3D models with MeshComponent and ModelRendererComponent.
/// This system operates on entities that have TransformComponent, MeshComponent, and ModelRendererComponent.
/// </summary>
public class ModelRenderingSystem : ISystem
{
    private readonly IGraphics3D _graphics3D;

    /// <summary>
    /// Gets the priority of this system.
    /// Priority 210 ensures 3D models render after 2D sprites (which typically use priority 200).
    /// </summary>
    public int Priority => 210;

    /// <summary>
    /// Initializes a new instance of the ModelRenderingSystem.
    /// </summary>
    /// <param name="graphics3D">The 3D graphics renderer to use for drawing models.</param>
    /// <exception cref="ArgumentNullException">Thrown when graphics3D is null.</exception>
    public ModelRenderingSystem(IGraphics3D graphics3D)
    {
        _graphics3D = graphics3D ?? throw new ArgumentNullException(nameof(graphics3D));
    }

    /// <summary>
    /// Called once when the system is registered and initialized.
    /// </summary>
    public void OnInit()
    {
        // No initialization needed for now
    }

    /// <summary>
    /// Called every frame to update and render 3D models.
    /// Finds the primary camera and renders all entities with model components.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update.</param>
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

        // Skip rendering if no active camera is set
        if (mainCamera == null)
            return;

        _graphics3D.BeginScene(mainCamera, cameraTransform);

        // Get all entities with the required components for 3D model rendering
        var group = Context.Instance.GetGroup([
            typeof(TransformComponent),
            typeof(MeshComponent),
            typeof(ModelRendererComponent)
        ]);

        foreach (var entity in group)
        {
            var transformComponent = entity.GetComponent<TransformComponent>();
            var meshComponent = entity.GetComponent<MeshComponent>();
            var modelRendererComponent = entity.GetComponent<ModelRendererComponent>();

            // Draw the model with entity ID for editor picking functionality
            _graphics3D.DrawModel(
                transformComponent.GetTransform(),
                meshComponent,
                modelRendererComponent,
                entity.Id
            );
        }

        _graphics3D.EndScene();
    }

    /// <summary>
    /// Called when the system is being shut down.
    /// </summary>
    public void OnShutdown()
    {
        // No cleanup needed for now
    }
}
