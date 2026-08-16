using ECS;
using ECS.Systems;
using Engine.Core;
using Engine.Physics;
using Engine.Renderer;
using Engine.Renderer.Pipeline;
using Serilog;

namespace Engine.Scene.Systems;

internal sealed class PhysicsDebugRenderSystem(
    IGraphics2D graphics2D,
    IContext context,
    DebugSettings debugSettings,
    PhysicsRuntimeBodyStore bodyStore,
    IPrimaryCameraProvider cameraProvider) : ISystem
{
    private static readonly ILogger Logger = Log.ForContext<PhysicsDebugRenderSystem>();

    public int Priority => SystemPriorities.PhysicsDebugRenderSystem;

    public void OnInit()
    {
        Logger.Debug("PhysicsDebugRenderSystem initialized with priority {Priority}", Priority);
    }

    public void OnUpdate(TimeSpan deltaTime)
    {
        if (!debugSettings.ShowColliderBounds)
            return;

        var camera = SceneRenderPipeline.CameraBinding.FromProvider(cameraProvider);
        PhysicsDebugDrawer.Draw(context, graphics2D, bodyStore, camera, useTransformFallbackWhenNoBody: false);
    }

    public void OnShutdown() { }
}
