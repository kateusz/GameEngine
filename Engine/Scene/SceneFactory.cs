using ECS;
using Engine.Core;
using Engine.Renderer;
using Engine.Renderer.Textures;

namespace Engine.Scene;

public sealed class SceneFactory(
    IGraphics2D graphics2D,
    IGraphics3D graphics3D,
    ITextureFactory textureFactory,
    IContext context,
    DebugSettings debugSettings,
    ISystemManagerFactory systemManagerFactory)
{
    public IScene Create(string path, string newSceneName)
    {
        var systemManager = systemManagerFactory.Create();
        return new Scene(path, newSceneName, graphics2D, graphics3D, textureFactory, context, debugSettings,
            systemManager);
    }
}
