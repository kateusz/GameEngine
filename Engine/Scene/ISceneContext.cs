namespace Engine.Scene;

/// <summary>
/// Interface for accessing the active scene and scene state.
/// Provides read-only access to scene information without circular dependencies.
/// </summary>
public interface ISceneContext
{
    /// <summary>
    /// Gets the currently active scene.
    /// </summary>
    IScene? ActiveScene { get; }

    /// <summary>
    /// Gets the current scene state (Edit or Play).
    /// </summary>
    SceneState State { get; }

    /// <summary>
    /// Event raised when the active scene changes.
    /// </summary>
    event Action<IScene?, IScene?>? SceneChanged;
}
