using ECS;
using Engine.Renderer;
using Engine.Renderer.Textures;

namespace Engine.Scene;

public sealed class SceneFactory(
    ISceneSystemRegistry sceneSystemRegistry,
    IGraphics2D graphics2D,
    IContext context,
    ITextureFactory textureFactory)
{
    public IScene Create(string path) => new Scene(path, sceneSystemRegistry, graphics2D, context, textureFactory);
}
