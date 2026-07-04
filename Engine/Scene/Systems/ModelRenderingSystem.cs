using ECS;
using ECS.Systems;
using Engine.Renderer;
using Engine.Scene;
using Serilog;

namespace Engine.Scene.Systems;

/// <summary>
/// System responsible for rendering 3D models
/// </summary>
internal sealed class ModelRenderingSystem(
    IGraphics3D graphics3D,
    IContext context,
    IPrimaryCameraProvider cameraProvider) : ISystem
{
    private static readonly ILogger Logger = Log.ForContext<ModelRenderingSystem>();

    public int Priority => SystemPriorities.ModelRenderSystem;

    public void OnInit()
    {
        Logger.Debug("ModelRenderingSystem initialized with priority {Priority}", Priority);
    }

    public void OnUpdate(TimeSpan deltaTime)
    {
        SceneRenderPipeline.RenderModels(
            context,
            graphics3D,
            SceneRenderPipeline.CameraBinding.FromProvider(cameraProvider));
    }

    public void OnShutdown() { }
}
