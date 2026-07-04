using ECS;

namespace Engine.Scene;

public sealed class SceneFactory(ISystemManagerFactory systemManagerFactory)
{
    public IScene Create(string path, string newSceneName)
    {
        var context = new Context();
        var build = systemManagerFactory.Create(context);
        return new Scene(path, newSceneName, context,
            build.SystemManager, build.BodyStore, build.ContactQueue, build.ScriptStore);
    }
}
