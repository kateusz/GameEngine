namespace Engine.Scene;

/// <summary>
/// Singleton class that provides global access to the current scene.
/// This allows any script or component to access the active scene.
/// </summary>
public static class CurrentScene
{
    private static IScene? _instance;

    /// <summary>
    /// Gets the current scene instance. Returns null if no scene is active.
    /// </summary>
    public static IScene? Instance => _instance;

    /// <summary>
    /// Sets the current scene instance. This should be called when a scene is loaded or created.
    /// </summary>
    /// <param name="scene">The scene to set as current</param>
    public static void Set(IScene scene)
    {
        _instance = scene;
    }

    /// <summary>
    /// Clears the current scene instance. This should be called when a scene is unloaded.
    /// </summary>
    public static void Clear()
    {
        _instance = null;
    }
} 