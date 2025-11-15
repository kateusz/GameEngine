using ECS;
using Engine.Renderer;

namespace Engine.Scene;

/// <summary>
/// Factory for creating Scene instances with proper dependency injection.
/// Each scene gets its own dedicated Context instance to ensure proper isolation.
/// </summary>
public class SceneFactory
{
    private readonly ISceneSystemRegistry _sceneSystemRegistry;
    private readonly IGraphics2D _graphics2D;

    public SceneFactory(ISceneSystemRegistry sceneSystemRegistry, IGraphics2D graphics2D)
    {
        _sceneSystemRegistry = sceneSystemRegistry;
        _graphics2D = graphics2D;
    }

    /// <summary>
    /// Creates a new Scene instance with a dedicated Context for entity management.
    /// Each scene maintains its own isolated entity registry, preventing cross-scene interference.
    /// </summary>
    /// <param name="path">Path to the scene file</param>
    /// <returns>A new Scene instance with dedicated Context</returns>
    public IScene Create(string path)
    {
        // Create a new Context instance per scene to ensure proper isolation
        var context = new Context();
        return new Scene(path, _sceneSystemRegistry, _graphics2D, context);
    }
}