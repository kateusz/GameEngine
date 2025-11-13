namespace Engine.Scene;

/// <summary>
/// Manages the current active scene instance.
/// Registered as a singleton in the IoC container to provide global access.
/// </summary>
public class CurrentSceneManager : ICurrentScene
{
    private IScene? _instance;

    /// <summary>
    /// Gets the current scene instance. Returns null if no scene is active.
    /// </summary>
    public IScene? Instance => _instance;

    /// <summary>
    /// Sets the current scene instance. This should be called when a scene is loaded or created.
    /// </summary>
    /// <param name="scene">The scene to set as current</param>
    public void Set(IScene scene)
    {
        _instance = scene;
    }

    /// <summary>
    /// Clears the current scene instance. This should be called when a scene is unloaded.
    /// </summary>
    public void Clear()
    {
        _instance = null;
    }
}
