using System.Numerics;

namespace Editor.Features.Settings;

/// <summary>
/// Interface for managing editor preferences including recent projects list and editor settings.
/// Persists data to disk.
/// </summary>
public interface IEditorPreferences : IDisposable
{
    /// <summary>
    /// Gets or sets the list of recent projects.
    /// </summary>
    List<RecentProject> RecentProjects { get; init; }

    /// <summary>
    /// Gets or sets the editor viewport background color.
    /// </summary>
    Vector4 BackgroundColor { get; set; }

    /// <summary>
    /// Gets or sets whether to show collider bounds in the viewport.
    /// </summary>
    bool ShowColliderBounds { get; set; }

    /// <summary>
    /// Gets or sets whether to show FPS counter.
    /// </summary>
    bool ShowFPS { get; set; }

    /// <summary>
    /// Gets or sets whether bloom post-processing is enabled.
    /// </summary>
    bool BloomEnabled { get; set; }

    /// <summary>
    /// Gets or sets bloom extraction threshold.
    /// </summary>
    float BloomThreshold { get; set; }

    /// <summary>
    /// Gets or sets bloom extraction knee for soft transitions.
    /// </summary>
    float BloomSoftKnee { get; set; }

    /// <summary>
    /// Gets or sets bloom intensity added during composite.
    /// </summary>
    float BloomIntensity { get; set; }

    /// <summary>
    /// Gets or sets number of blur passes for bloom.
    /// </summary>
    int BloomBlurPasses { get; set; }

    /// <summary>
    /// Gets or sets bloom downsample factor.
    /// </summary>
    int BloomDownsampleFactor { get; set; }

    /// <summary>
    /// Gets or sets bloom composite exposure.
    /// </summary>
    float BloomExposure { get; set; }

    /// <summary>
    /// Gets or sets bloom composite gamma correction.
    /// </summary>
    float BloomGamma { get; set; }

    /// <summary>
    /// Adds a project to the recent projects list, moving it to the front if already present.
    /// Automatically saves preferences after update.
    /// </summary>
    /// <param name="path">Absolute path to the project directory.</param>
    /// <param name="name">Display name of the project.</param>
    void AddRecentProject(string path, string name);

    /// <summary>
    /// Removes a project from the recent projects list (e.g., if deleted or invalid).
    /// Automatically saves preferences after update.
    /// </summary>
    /// <param name="path">Absolute path to the project directory.</param>
    void RemoveRecentProject(string path);

    /// <summary>
    /// Gets a recent projects list.
    /// </summary>
    /// <returns>A read-only copy of the recent projects list.</returns>
    IReadOnlyList<RecentProject> GetRecentProjects();

    /// <summary>
    /// Clears all recent projects from the list.
    /// Automatically saves preferences after update.
    /// </summary>
    void ClearRecentProjects();

    /// <summary>
    /// Saves preferences to disk in JSON format asynchronously.
    /// Debounces rapid save calls to avoid excessive I/O.
    /// </summary>
    void Save();
}
