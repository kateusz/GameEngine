using ECS;

namespace Editor.Features.Scene;

/// <summary>
/// Interface for managing scene lifecycle in the editor.
/// Handles scene creation, loading, saving, and play/edit mode transitions.
/// </summary>
public interface ISceneManager
{
    /// <summary>
    /// Creates a new empty scene.
    /// </summary>
    void New();

    /// <summary>
    /// Opens an existing scene from the specified path.
    /// </summary>
    /// <param name="path">Path to the scene file</param>
    void Open(string path);

    /// <summary>
    /// Saves the current scene to disk.
    /// </summary>
    /// <param name="scenesDir">Optional directory to save the scene in. If null, uses default assets/scenes directory.</param>
    void Save(string? scenesDir);

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

    /// <summary>
    /// Duplicates the currently selected entity in the scene hierarchy.
    /// Only works in edit mode.
    /// </summary>
    void DuplicateEntity(Entity entity);

    /// <summary>
    /// Gets the current scene file path.
    /// </summary>
    /// <returns>The path to the current scene file, or null if no scene is loaded or saved.</returns>
    string? GetCurrentScenePath();
}
