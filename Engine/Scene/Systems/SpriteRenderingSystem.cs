using ECS;
using ECS.Systems;
using Engine.Renderer;
using Engine.Renderer.Textures;
using Engine.Scene;
using Serilog;

namespace Engine.Scene.Systems;

/// <summary>
/// System responsible for rendering 2D sprites.
/// </summary>
internal sealed class SpriteRenderingSystem(
    IGraphics2D renderer,
    ITextureFactory? textureFactory,
    IContext context,
    IPrimaryCameraProvider cameraProvider) : ISystem
{
    private static readonly ILogger Logger = Log.ForContext<SpriteRenderingSystem>();

    public int Priority => SystemPriorities.SpriteRenderSystem;

    public void OnInit()
    {
        Logger.Debug("SpriteRenderingSystem initialized with priority {Priority}", Priority);
    }

    public void OnUpdate(TimeSpan deltaTime)
    {
        SceneRenderPipeline.RenderSprites(
            context,
            renderer,
            textureFactory,
            SceneRenderPipeline.CameraBinding.FromProvider(cameraProvider));
    }

    public void OnShutdown() { }
}
