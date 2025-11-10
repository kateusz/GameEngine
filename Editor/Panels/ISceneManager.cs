using System.Numerics;
using Engine.Renderer.Cameras;
using Engine.Scene;

namespace Editor.Panels;

/// <summary>
/// Interface for managing scene lifecycle in the editor.
/// Handles scene creation, loading, saving, and play/edit mode transitions.
/// </summary>
public interface ISceneManager
{
    /// <summary>
    /// Gets the current scene state (Edit or Play).
    /// </summary>
    SceneState SceneState { get; }

    /// <summary>
    /// Gets the file path of the currently loaded editor scene.
    /// </summary>
    string? EditorScenePath { get; }

    /// <summary>
    /// Creates a new empty scene.
    /// </summary>
    /// <param name="viewportSize">The size of the viewport for camera setup</param>
    void New(Vector2 viewportSize);

    /// <summary>
    /// Opens an existing scene from the specified path.
    /// </summary>
    /// <param name="viewportSize">The size of the viewport for camera setup</param>
    /// <param name="path">Path to the scene file</param>
    void Open(Vector2 viewportSize, string path);

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
    /// Duplicates the currently selected entity in the scene hierarchy.
    /// Only works in edit mode.
    /// </summary>
    void DuplicateEntity();

    /// <summary>
    /// Moves the editor camera to focus on the currently selected entity.
    /// </summary>
    /// <param name="cameraController">The camera controller to update</param>
    void FocusOnSelectedEntity(OrthographicCameraController cameraController);
}
