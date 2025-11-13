namespace Engine.Scene;

/// <summary>
/// Interface for accessing the current active scene.
/// This is registered as a singleton in the IoC container.
/// </summary>
public interface ICurrentScene
{
    /// <summary>
    /// Gets the current scene instance. Returns null if no scene is active.
    /// </summary>
    IScene? Instance { get; }

    /// <summary>
    /// Sets the current scene instance. This should be called when a scene is loaded or created.
    /// </summary>
    /// <param name="scene">The scene to set as current</param>
    void Set(IScene scene);

    /// <summary>
    /// Clears the current scene instance. This should be called when a scene is unloaded.
    /// </summary>
    void Clear();
}
