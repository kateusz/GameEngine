using ECS;
using ECS.Systems;
using Engine.Scene.Systems;

namespace Engine.Scene;

internal sealed class SystemManagerFactory(ISceneSystemsFactory sceneSystemsFactory) : ISystemManagerFactory
{
    public SceneBuildResult Create(IContext context)
    {
        var bodyStore = new PhysicsRuntimeBodyStore();
        var contactQueue = new PhysicsContactQueue();
        var systemManager = new SystemManager();
        sceneSystemsFactory.PopulateSystemManager(systemManager, context, bodyStore, contactQueue);
        return new SceneBuildResult(systemManager, bodyStore, contactQueue);
    }
}
