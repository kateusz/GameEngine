using ECS;
using ECS.Systems;
using Engine.Core;
using Engine.Core.Window;
using Engine.Scene.Systems;
using Scripting;

namespace Engine.Scene;

[SkipUnitTests]
public sealed class SceneFactory(ISceneSystemsFactory sceneSystemsFactory, IPointerSurface pointerSurface)
{
    public IScene Create(string newSceneName, SceneDimension dimension = SceneDimension.TwoD)
    {
        var context = new Context();
        var bodyStore = new PhysicsRuntimeBodyStore();
        var contactQueue = new PhysicsContactQueue();
        var systemManager = new SystemManager();
        var physicsQueries = sceneSystemsFactory.PopulateSystemManager(
            systemManager, context, bodyStore, contactQueue);
        ICameraQueries cameraQueries = new CameraQueries(context, pointerSurface);
        var scene = new Scene(newSceneName, context,
            systemManager, bodyStore, contactQueue, physicsQueries,
            cameraQueries);
        scene.Dimension = dimension;
        return scene;
    }
}
