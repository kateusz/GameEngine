using ECS;
using Engine.Core;
using Engine.Core.Window;
using Engine.Scene.Systems;
using Scripting;

namespace Engine.Scene;

[SkipUnitTests]
public sealed class SceneFactory(ISystemManagerFactory systemManagerFactory, IPointerSurface pointerSurface)
{
    public IScene Create(string path, string newSceneName)
    {
        var context = new Context();
        var build = systemManagerFactory.Create(context);
        ICameraQueries cameraQueries = new CameraQueries(context, pointerSurface);
        return new Scene(path, newSceneName, context,
            build.SystemManager, build.BodyStore, build.ContactQueue, build.ScriptStore, build.PhysicsWorld,
            cameraQueries);
    }
}
