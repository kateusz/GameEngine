using System.Runtime.InteropServices;
using System.Text.Json;
using Serilog;

namespace Editor;

/// <summary>
/// Represents a recently opened project with metadata.
/// </summary>
public class RecentProject
{
    public string Path { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime LastOpened { get; set; }
}

/// <summary>
/// Manages editor preferences including recent projects list.
/// Persists data to AppData/GameEngine/editor-preferences.json
/// </summary>
public class EditorPreferences
{
    private static readonly ILogger Logger = Log.ForContext<EditorPreferences>();
    
    public int Version { get; set; } = 1;
    public List<RecentProject> RecentProjects { get; set; } = new();
    public const int MaxRecentProjects = 10;

    private static readonly string PreferencesPath =
        System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GameEngine",
            "editor-preferences.json"
        );

    private static readonly StringComparison PathComparison =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

    private readonly object _lock = new();
    private readonly SemaphoreSlim _saveSemaphore = new(1, 1);
    private CancellationTokenSource? _pendingSaveCts;

    /// <summary>
    /// Adds a project to the recent projects list, moving it to the front if already present.
    /// Automatically saves preferences after update.
    /// </summary>
    /// <param name="path">Absolute path to the project directory.</param>
    /// <param name="name">Display name of the project.</param>
    public void AddRecentProject(string path, string name)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or whitespace", nameof(path));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or whitespace", nameof(name));

        lock (_lock)
        {
            // Normalize path to prevent duplicates
            path = System.IO.Path.GetFullPath(path);

            // Remove existing entry if present
            var existing = RecentProjects.FirstOrDefault(p =>
                string.Equals(p.Path, path, PathComparison));
            if (existing != null)
                RecentProjects.Remove(existing);

            // Add to front of list
            RecentProjects.Insert(0, new RecentProject
            {
                Path = path,
                Name = name,
                LastOpened = DateTime.UtcNow
            });

            // Trim to max size
            if (RecentProjects.Count > MaxRecentProjects)
                RecentProjects.RemoveRange(MaxRecentProjects,
                                          RecentProjects.Count - MaxRecentProjects);
        }

        Save();
    }

    /// <summary>
    /// Removes a project from the recent projects list (e.g., if deleted or invalid).
    /// Automatically saves preferences after update.
    /// </summary>
    /// <param name="path">Absolute path to the project directory.</param>
    public void RemoveRecentProject(string path)
    {
        lock (_lock)
        {
            path = System.IO.Path.GetFullPath(path);
            RecentProjects.RemoveAll(p =>
                string.Equals(p.Path, path, PathComparison));
        }
        Save();
    }

    /// <summary>
    /// Gets a thread-safe snapshot of the recent projects list.
    /// </summary>
    /// <returns>A read-only copy of the recent projects list.</returns>
    public IReadOnlyList<RecentProject> GetRecentProjects()
    {
        lock (_lock)
        {
            return RecentProjects.ToList();
        }
    }

    /// <summary>
    /// Clears all recent projects from the list.
    /// Automatically saves preferences after update.
    /// </summary>
    public void ClearRecentProjects()
    {
        lock (_lock)
        {
            RecentProjects.Clear();
        }
        Save();
    }

    /// <summary>
    /// Saves preferences to disk in JSON format asynchronously.
    /// Debounces rapid save calls to avoid excessive I/O.
    /// </summary>
    public void Save()
    {
        // Cancel any pending save and schedule a new one
        _pendingSaveCts?.Cancel();
        _pendingSaveCts = new CancellationTokenSource();
        _ = SaveAsync(_pendingSaveCts.Token);
    }

    private async Task SaveAsync(CancellationToken ct)
    {
        try
        {
            // Debounce rapid saves
            await Task.Delay(100, ct).ConfigureAwait(false);
            if (ct.IsCancellationRequested) return;

            await _saveSemaphore.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                string json;
                lock (_lock)
                {
                    json = JsonSerializer.Serialize(this, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                }

                var directory = System.IO.Path.GetDirectoryName(PreferencesPath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory!);

                await File.WriteAllTextAsync(PreferencesPath, json, ct).ConfigureAwait(false);
                Logger.Debug("Editor preferences saved to {Path}", PreferencesPath);
            }
            finally
            {
                _saveSemaphore.Release();
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when debouncing, no action needed
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to save editor preferences to {Path}", PreferencesPath);
        }
    }

    /// <summary>
    /// Loads preferences from disk, or returns a new instance if file doesn't exist or fails to load.
    /// </summary>
    public static EditorPreferences Load()
    {
        try
        {
            if (File.Exists(PreferencesPath))
            {
                var json = File.ReadAllText(PreferencesPath);
                var prefs = JsonSerializer.Deserialize<EditorPreferences>(json);

                if (prefs == null)
                {
                    Logger.Warning("Failed to deserialize preferences, using defaults");
                    return new EditorPreferences();
                }

                // Version migration logic can be added here if needed in the future
                if (prefs.Version < 1)
                {
                    Logger.Information("Migrating preferences from version {Old} to {New}",
                        prefs.Version, 1);
                    // Perform migration if needed
                }

                Logger.Information("Editor preferences loaded from {Path}", PreferencesPath);
                return prefs;
            }

            Logger.Information("No preferences file found, using defaults");
        }
        catch (JsonException ex)
        {
            Logger.Error(ex, "Corrupted preferences file, resetting to defaults");
            // Optionally backup corrupted file
            try
            {
                File.Move(PreferencesPath, PreferencesPath + ".corrupted");
                Logger.Information("Corrupted preferences backed up to {Path}", PreferencesPath + ".corrupted");
            }
            catch
            {
                // Ignore backup failure
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load editor preferences from {Path}", PreferencesPath);
        }

        return new EditorPreferences();
    }
}
