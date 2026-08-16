using ECS;
using Engine.Core;
using Engine.Core.Window;
using Engine.Scene.Systems;
using Scripting;

namespace Engine.Scene;

[SkipUnitTests]
public sealed class SceneFactory(ISystemManagerFactory systemManagerFactory, IPointerSurface pointerSurface)
{
    public IScene Create(string path, string newSceneName, SceneDimension dimension = SceneDimension.TwoD)
    {
        var context = new Context();
        var build = systemManagerFactory.Create(context, dimension);
        ICameraQueries cameraQueries = new CameraQueries(context, pointerSurface);
        var scene = new Scene(path, newSceneName, context,
            build.SystemManager, build.BodyStore, build.ContactQueue, build.ScriptStore, build.PhysicsQueries,
            cameraQueries, build.BodyStore3D);
        scene.Dimension = dimension;
        return scene;
    }
}
