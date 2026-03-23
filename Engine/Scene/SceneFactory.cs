using ECS;
using Engine.Core;
using Engine.Renderer;

namespace Engine.Scene;

public sealed class SceneFactory(
    ISceneSystemRegistry sceneSystemRegistry,
    IGraphics2D graphics2D,
    IGraphics3D graphics3D,
    IContext context,
    DebugSettings debugSettings)
{
    public IScene Create(string path) => new Scene(path, sceneSystemRegistry, graphics2D, graphics3D, context, debugSettings);
}
