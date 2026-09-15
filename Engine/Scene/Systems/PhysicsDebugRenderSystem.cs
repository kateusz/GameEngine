using ECS;
using ECS.Systems;
using Engine.Core;
using Engine.Physics;
using Engine.Renderer.Pipeline;
using Serilog;

namespace Engine.Scene.Systems;

internal sealed class PhysicsDebugRenderSystem(
    IGraphics2D graphics2D,
    IContext context,
    DebugSettings debugSettings,
    PhysicsRuntimeBodyStore bodyStore) : ISystem
{
    private static readonly ILogger Logger = Log.ForContext<PhysicsDebugRenderSystem>();
    private readonly SceneCamera _scratchCamera = new();

    public int Priority => SystemPriorities.PhysicsDebugRenderSystem;

    public void OnInit()
    {
        Logger.Debug("PhysicsDebugRenderSystem initialized with priority {Priority}", Priority);
    }

    public void OnUpdate(TimeSpan deltaTime)
    {
        if (!debugSettings.ShowColliderBounds)
            return;

        if (!CameraQueries.TryGetPrimaryView(context, _scratchCamera, out var view))
            return;

        PhysicsDebugDrawer.Draw(
            context, graphics2D, bodyStore,
            view,
            useTransformFallbackWhenNoBody: false);
    }

    public void OnShutdown() { }
}
