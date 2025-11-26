using ECS;
using Engine.Renderer;

namespace Engine.Scene;

public class SceneFactory
{
    private readonly ISceneSystemRegistry _sceneSystemRegistry;
    private readonly IGraphics2D _graphics2D;
    private readonly IContext _context;

    public SceneFactory(ISceneSystemRegistry sceneSystemRegistry, IGraphics2D graphics2D, IContext context)
    {
        _sceneSystemRegistry = sceneSystemRegistry;
        _graphics2D = graphics2D;
        _context = context;
    }

    public IScene Create(string path) => new Scene(path, _sceneSystemRegistry, _graphics2D, _context);
}