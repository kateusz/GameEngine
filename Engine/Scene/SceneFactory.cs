using ECS;
using Engine.Core;
using Engine.Renderer;
using Engine.Renderer.Textures;

namespace Engine.Scene;

public sealed class SceneFactory(
    IGraphics2D graphics2D,
    IGraphics3D graphics3D,
    ITextureFactory textureFactory,
    DebugSettings debugSettings,
    ISystemManagerFactory systemManagerFactory)
{
    public IScene Create(string path, string newSceneName)
    {
        var context = new Context();
        var build = systemManagerFactory.Create(context);
        return new Scene(path, newSceneName, graphics2D, graphics3D, textureFactory, context, debugSettings,
            build.SystemManager, build.BodyStore, build.ContactQueue, build.ScriptStore);
    }
}
