using ECS;
using ECS.Systems;
using Engine.Renderer;
using Engine.Renderer.Textures;
using Serilog;

namespace Engine.Scene.Systems;

internal sealed class Scene2DRenderSystem(
    IGraphics2D renderer,
    ITextureFactory? textureFactory,
    IContext context,
    IPrimaryCameraProvider cameraProvider) : ISystem
{
    private static readonly ILogger Logger = Log.ForContext<Scene2DRenderSystem>();

    public int Priority => SystemPriorities.Scene2DRenderSystem;

    public void OnInit()
    {
        Logger.Debug("Scene2DRenderSystem initialized with priority {Priority}", Priority);
    }

    public void OnUpdate(TimeSpan deltaTime)
    {
        SceneRenderPipeline.RenderSpritesAndSubTextures(
            context,
            renderer,
            textureFactory,
            SceneRenderPipeline.CameraBinding.FromProvider(cameraProvider));
    }

    public void OnShutdown() { }
}
