using ECS;

namespace Editor.Features.Scene;

/// <summary>
/// Interface for managing scene lifecycle in the editor.
/// Handles scene creation, loading, saving, and play/edit mode transitions.
/// </summary>
public interface ISceneManager
{
    /// <summary>
    /// Creates a new empty scene and saves it immediately when a project is open.
    /// </summary>
    /// <param name="sceneName"></param>
    void New(string sceneName);

    /// <summary>
    /// Opens an existing scene from the specified path.
    /// </summary>
    /// <param name="path">Path to the scene file</param>
    void Open(string path);

    /// <summary>
    /// Saves the current scene to disk.
    /// </summary>
    /// <param name="compileScripts">When true, compiles/applies scripts before serialize (Ctrl+S). Autosave passes false.</param>
    void Save(bool compileScripts = true);

    void Close();

    bool IsDirty { get; }

    /// <summary>
    /// Enters play mode, initializing runtime systems and physics.
    /// </summary>
    void Play();

    /// <summary>
    /// Runs deferred play/stop queued by <see cref="Play"/>, <see cref="Stop"/>, or <see cref="Restart"/>.
    /// Call once per frame from the editor update loop (not from ImGui draw handlers).
    /// </summary>
    void FlushPendingRuntimeStart();

    /// <summary>
    /// Exits play mode, returning to edit mode and stopping runtime systems.
    /// Preserves the current scene state so play can be resumed.
    /// </summary>
    void Stop();

    /// <summary>
    /// Reloads the scene from the play-mode snapshot and starts play mode.
    /// Requires that play mode was entered at least once in this session.
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
