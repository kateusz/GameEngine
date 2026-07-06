using ECS;
using ECS.Systems;
using Engine.Core;
using Engine.Scene.Systems;

namespace Engine.Scene;

[SkipUnitTests]
internal sealed class SystemManagerFactory(ISceneSystemsFactory sceneSystemsFactory) : ISystemManagerFactory
{
    public SceneBuildResult Create(IContext context)
    {
        var bodyStore = new PhysicsRuntimeBodyStore();
        var contactQueue = new PhysicsContactQueue();
        var scriptStore = new ScriptRuntimeStore();
        var systemManager = new SystemManager();
        sceneSystemsFactory.PopulateSystemManager(systemManager, context, bodyStore, contactQueue, scriptStore);
        return new SceneBuildResult(systemManager, bodyStore, contactQueue, scriptStore);
    }
}
