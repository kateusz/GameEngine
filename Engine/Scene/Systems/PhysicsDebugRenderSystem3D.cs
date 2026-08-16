using ECS;
using ECS.Systems;
using Engine.Core;
using Engine.Physics;
using Engine.Renderer.Pipeline;
using Engine.Scene;
using Serilog;

namespace Engine.Scene.Systems;

internal sealed class PhysicsDebugRenderSystem3D(
    IGraphics3D graphics3D,
    IContext context,
    DebugSettings debugSettings,
    PhysicsRuntimeBodyStore3D bodyStore,
    IPrimaryCameraProvider cameraProvider) : ISystem
{
    private static readonly ILogger Logger = Log.ForContext<PhysicsDebugRenderSystem3D>();

    public int Priority => SystemPriorities.PhysicsDebugRenderSystem;

    public void OnInit()
    {
        Logger.Debug("PhysicsDebugRenderSystem3D initialized with priority {Priority}", Priority);
    }

    public void OnUpdate(TimeSpan deltaTime)
    {
        if (!debugSettings.ShowColliderBounds)
            return;

        var camera = SceneRenderPipeline.CameraBinding.FromProvider(cameraProvider);
        PhysicsDebugDrawer3D.Draw(context, graphics3D, bodyStore, camera, useTransformFallbackWhenNoBody: false);
    }

    public void OnShutdown() { }
}
