using ECS;
using ECS.Systems;
using Engine.Core;
using Engine.Renderer;
using Engine.Scene;
using Serilog;

namespace Engine.Scene.Systems;

/// <summary>
/// System responsible for rendering physics debug visualizations.
/// Draws wireframe overlays for collision shapes to aid in debugging physics behavior.
/// </summary>
internal sealed class PhysicsDebugRenderSystem(
    IGraphics2D renderer,
    IContext context,
    DebugSettings debugSettings,
    IPrimaryCameraProvider cameraProvider,
    PhysicsRuntimeBodyStore bodyStore) : ISystem
{
    private static readonly ILogger Logger = Log.ForContext<PhysicsDebugRenderSystem>();

    public int Priority => SystemPriorities.PhysicsDebugRenderSystem;

    public void OnInit()
    {
        Logger.Debug("PhysicsDebugRenderSystem initialized with priority {Priority}", Priority);
    }

    public void OnUpdate(TimeSpan deltaTime)
    {
        SceneRenderPipeline.RenderPhysicsDebug(
            context,
            renderer,
            debugSettings,
            bodyStore,
            SceneRenderPipeline.CameraBinding.FromProvider(cameraProvider),
            useTransformFallbackWhenNoBody: false);
    }

    public void OnShutdown() { }
}
