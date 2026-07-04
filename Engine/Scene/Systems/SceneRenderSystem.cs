using ECS;
using ECS.Systems;
using Engine.Core;
using Engine.Renderer;
using Engine.Renderer.Textures;
using Serilog;

namespace Engine.Scene.Systems;

internal sealed class SceneRenderSystem(
    IGraphics2D graphics2D,
    IGraphics3D graphics3D,
    ITextureFactory textureFactory,
    IContext context,
    DebugSettings debugSettings,
    PhysicsRuntimeBodyStore bodyStore,
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
        SceneRenderPipeline.RenderScene(
            context,
            graphics2D,
            graphics3D,
            textureFactory,
            debugSettings,
            bodyStore,
            SceneRenderPipeline.CameraBinding.FromProvider(cameraProvider),
            useTransformFallbackWhenNoBody: false);
    }

    public void OnShutdown() { }
}
