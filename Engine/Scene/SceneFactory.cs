using Engine.Renderer;

namespace Engine.Scene;

public class SceneFactory
{
    private readonly SceneSystemRegistry _sceneSystemRegistry;
    private readonly IGraphics2D _graphics2D;

    public SceneFactory(SceneSystemRegistry sceneSystemRegistry, IGraphics2D graphics2D)
    {
        _sceneSystemRegistry = sceneSystemRegistry;
        _graphics2D = graphics2D;
    }

    public Scene Create(string path)
    {
        return new Scene(path, _sceneSystemRegistry, _graphics2D);
    }
}