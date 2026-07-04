using ECS;
using ECS.Systems;
using Engine.Renderer;
using Engine.Renderer.Textures;
using Engine.Scene;
using Serilog;

namespace Engine.Scene.Systems;

/// <summary>
/// System responsible for rendering 2D subtextures (sprite atlas/sprite sheet regions).
/// </summary>
internal sealed class SubTextureRenderingSystem(
    IGraphics2D renderer,
    ITextureFactory? textureFactory,
    IContext context,
    IPrimaryCameraProvider cameraProvider) : ISystem
{
    private static readonly ILogger Logger = Log.ForContext<SubTextureRenderingSystem>();

    public int Priority => SystemPriorities.SubTextureRenderSystem;

    public void OnInit()
    {
        Logger.Debug("SubTextureRenderingSystem initialized with priority {Priority}", Priority);
    }

    public void OnUpdate(TimeSpan deltaTime)
    {
        SceneRenderPipeline.RenderSubTextures(
            context,
            renderer,
            textureFactory,
            SceneRenderPipeline.CameraBinding.FromProvider(cameraProvider));
    }

    public void OnShutdown() { }
}
