using ECS;
using ECS.Systems;
using Engine.Core;
using Engine.Physics;
using Engine.Renderer.Pipeline;

namespace Engine.Scene.Systems;

internal sealed class PhysicsDebugRenderSystem(
    IGraphics2D graphics2D,
    IContext context,
    DebugSettings debugSettings,
    PhysicsRuntimeBodyStore bodyStore) : ISystem
{
    public int Priority => 151;

    public void OnUpdate(TimeSpan deltaTime)
    {
        if (!debugSettings.ShowColliderBounds)
            return;

        if (!CameraQueries.TryGetPrimaryView(context, out var view))
            return;

        graphics2D.BeginScene(view);
        PhysicsDebugDrawer.DrawColliders(
            context, graphics2D, bodyStore,
            useTransformFallbackWhenNoBody: false);
        graphics2D.EndScene();
    }
}
