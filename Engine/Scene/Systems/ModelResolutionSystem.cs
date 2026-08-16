using ECS;
using ECS.Systems;
using Engine.Renderer;

namespace Engine.Scene.Systems;

internal sealed class ModelResolutionSystem(IContext context, IModelFactory modelFactory) : ISystem
{
    public int Priority => SystemPriorities.ModelResolutionSystem;

    public void OnInit() { }

    public void OnUpdate(TimeSpan deltaTime) =>
        ModelAssetResolver.SyncAll(context, modelFactory);

    public void OnShutdown() { }
}
