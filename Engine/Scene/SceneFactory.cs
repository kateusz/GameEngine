using ECS;
using Engine.Renderer;
using Engine.Renderer.Textures;

namespace Engine.Scene;

public class SceneFactory
{
    private readonly ISceneSystemRegistry _sceneSystemRegistry;
    private readonly IGraphics2D _graphics2D;
    private readonly IContext _context;
    private readonly ITextureFactory _textureFactory;

    public SceneFactory(ISceneSystemRegistry sceneSystemRegistry, IGraphics2D graphics2D, IContext context, ITextureFactory textureFactory)
    {
        _sceneSystemRegistry = sceneSystemRegistry;
        _graphics2D = graphics2D;
        _context = context;
        _textureFactory = textureFactory;
    }

    public IScene Create(string path) => new Scene(path, _sceneSystemRegistry, _graphics2D, _context, _textureFactory);
}