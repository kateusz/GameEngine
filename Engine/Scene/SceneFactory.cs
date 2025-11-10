using Engine.Renderer;

namespace Engine.Scene;

public class SceneFactory
{
    private readonly ISceneSystemRegistry _sceneSystemRegistry;
    private readonly IGraphics2D _graphics2D;

    public SceneFactory(ISceneSystemRegistry sceneSystemRegistry, IGraphics2D graphics2D)
    {
        _sceneSystemRegistry = sceneSystemRegistry;
        _graphics2D = graphics2D;
    }

    public IScene Create(string path)
    {
        return new Scene(path, _sceneSystemRegistry, _graphics2D);
    }
}