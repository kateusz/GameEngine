namespace Editor.Panels;

/// <summary>
/// Interface for managing editor scene playback state.
/// Handles transitions between Edit and Play modes.
/// </summary>
public interface IEditorSceneManager
{
    /// <summary>
    /// Enters play mode, initializing runtime systems and physics.
    /// </summary>
    void Play();

    /// <summary>
    /// Exits play mode, returning to edit mode and stopping runtime systems.
    /// </summary>
    void Stop();

    /// <summary>
    /// Restarts the scene by stopping and immediately starting play mode again.
    /// Only works when already in play mode.
    /// </summary>
    void Restart();
}
