using ECS;
using ECS.Systems;
using Engine.Core;
using Engine.Renderer;
using Engine.Renderer.Textures;

namespace Engine.Scene;

public sealed class SceneFactory(
    ISceneSystemRegistry sceneSystemRegistry,
    IGraphics2D graphics2D,
    IGraphics3D graphics3D,
    ITextureFactory textureFactory,
    IContext context,
    DebugSettings debugSettings,
    ISystemManager systemManager)
{
    public IScene Create(string path, string newSceneName) => new Scene(path, newSceneName, sceneSystemRegistry,
        graphics2D, graphics3D, textureFactory, context, debugSettings, systemManager);
}