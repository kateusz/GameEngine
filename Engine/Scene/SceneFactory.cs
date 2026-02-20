using ECS;
using Engine.Renderer;

namespace Engine.Scene;

public sealed class SceneFactory(
    ISceneSystemRegistry sceneSystemRegistry,
    IGraphics2D graphics2D,
    IContext context)
{
    public IScene Create(string path) => new Scene(path, sceneSystemRegistry, graphics2D, context);
}
