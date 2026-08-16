using ECS;
using ECS.Systems;
using Engine.Core;
using Engine.Scene.Systems;

namespace Engine.Scene;

[SkipUnitTests]
internal sealed class SystemManagerFactory(ISceneSystemsFactory sceneSystemsFactory) : ISystemManagerFactory
{
    public SceneBuildResult Create(IContext context, SceneDimension dimension = SceneDimension.TwoD)
    {
        var bodyStore = new PhysicsRuntimeBodyStore();
        // ponytail: unused 2D store on 3D scenes; dual-nullable stores if a second backend cares
        var bodyStore3D = dimension == SceneDimension.ThreeD ? new PhysicsRuntimeBodyStore3D() : null;
        var contactQueue = new PhysicsContactQueue();
        var scriptStore = new ScriptRuntimeStore();
        var systemManager = new SystemManager();
        var physicsWorld = sceneSystemsFactory.PopulateSystemManager(
            systemManager, context, bodyStore, bodyStore3D, contactQueue, scriptStore, dimension);
        return new SceneBuildResult(systemManager, bodyStore, contactQueue, scriptStore, physicsWorld, bodyStore3D);
    }
}
