using ECS;
using ECS.Systems;
using Engine.Renderer.Models;
using Engine.Renderer.Pipeline;
using Engine.Renderer.Textures;
using Engine.Scene.Cameras;
using Serilog;

namespace Engine.Scene.Systems;

internal sealed class SceneRenderSystem(
    IGraphics2D graphics2D,
    IGraphics3D graphics3D,
    ITextureFactory textureFactory,
    IContext context,
    IPrimaryCameraProvider cameraProvider,
    IModelFactory modelFactory) : ISystem
{
    private static readonly ILogger Logger = Log.ForContext<SceneRenderSystem>();

    public int Priority => SystemPriorities.SceneRenderSystem;

    public void OnInit()
    {
        Logger.Debug("SceneRenderSystem initialized with priority {Priority}", Priority);
    }

    public void OnUpdate(TimeSpan deltaTime)
    {
        if (cameraProvider.Camera is not { } cam)
            return;

        SceneRenderPipeline.RenderScene(
            context, graphics2D, graphics3D, textureFactory, modelFactory,
            CameraViews.From(cam, cameraProvider.Transform));
    }

    public void OnShutdown() { }
}
