using ECS;
using ECS.Systems;
using Engine.Renderer.Pipeline;
using Engine.Renderer.Textures;
using Serilog;

namespace Engine.Scene.Systems;

internal sealed class SceneRenderSystem(
    IGraphics2D graphics2D,
    ITextureFactory textureFactory,
    IContext context,
    IPrimaryCameraProvider cameraProvider) : ISystem
{
    private static readonly ILogger Logger = Log.ForContext<SceneRenderSystem>();

    public int Priority => SystemPriorities.SceneRenderSystem;

    public void OnInit()
    {
        Logger.Debug("SceneRenderSystem initialized with priority {Priority}", Priority);
    }

    public void OnUpdate(TimeSpan deltaTime)
    {
        var camera = SceneRenderPipeline.CameraBinding.FromProvider(cameraProvider);
        SceneRenderPipeline.RenderScene(context, graphics2D, textureFactory, camera);
    }

    public void OnShutdown() { }
}
