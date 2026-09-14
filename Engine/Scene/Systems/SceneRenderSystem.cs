using ECS;
using ECS.Systems;
using Engine.Renderer.Models;
using Engine.Renderer.Pipeline;
using Engine.Renderer.Textures;
using Serilog;

namespace Engine.Scene.Systems;

internal sealed class SceneRenderSystem(
    IGraphics2D graphics2D,
    IGraphics3D graphics3D,
    ITextureFactory textureFactory,
    IContext context,
    IModelFactory modelFactory) : ISystem
{
    private static readonly ILogger Logger = Log.ForContext<SceneRenderSystem>();
    private readonly SceneCamera _scratchCamera = new();

    public int Priority => SystemPriorities.SceneRenderSystem;

    public void OnInit()
    {
        Logger.Debug("SceneRenderSystem initialized with priority {Priority}", Priority);
    }

    public void OnUpdate(TimeSpan deltaTime)
    {
        if (!CameraQueries.TryGetPrimaryView(context, _scratchCamera, out var view))
            return;

        SceneRenderPipeline.RenderScene(
            context, graphics2D, graphics3D, textureFactory, modelFactory,
            view);
    }

    public void OnShutdown() { }
}
