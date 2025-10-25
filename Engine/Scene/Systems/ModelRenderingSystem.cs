using System.Numerics;
using ECS;
using Engine.Renderer;
using Engine.Renderer.Cameras;
using Engine.Scene.Components;
using Serilog;

namespace Engine.Scene.Systems;

/// <summary>
/// System responsible for rendering 3D models with MeshComponent and ModelRendererComponent.
/// This system operates on entities that have TransformComponent, MeshComponent, and ModelRendererComponent.
/// </summary>
public class ModelRenderingSystem : ISystem
{
    private static readonly ILogger Logger = Log.ForContext<ModelRenderingSystem>();
    
    private readonly IGraphics3D _graphics3D;
    private Camera? _activeCamera;
    private Matrix4x4 _cameraTransform;

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
    /// Sets the active camera and its transform matrix.
    /// This must be called before OnUpdate to ensure proper rendering.
    /// </summary>
    /// <param name="camera">The camera to use for rendering. Can be null to skip rendering.</param>
    /// <param name="transform">The camera's transform matrix.</param>
    public void SetCamera(Camera? camera, Matrix4x4 transform)
    {
        _activeCamera = camera;
        _cameraTransform = transform;
    }

    /// <summary>
    /// Called once when the system is registered and initialized.
    /// </summary>
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
        // Skip rendering if no active camera is set
        if (_activeCamera == null)
            return;

        _graphics3D.BeginScene(_activeCamera, _cameraTransform);

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
