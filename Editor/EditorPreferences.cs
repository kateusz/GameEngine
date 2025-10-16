using System.Text.Json;

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
    public List<RecentProject> RecentProjects { get; set; } = new();
    public const int MaxRecentProjects = 10;

    private static readonly string PreferencesPath = 
        System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GameEngine",
            "editor-preferences.json"
        );

    /// <summary>
    /// Adds a project to the recent projects list, moving it to the front if already present.
    /// Automatically saves preferences after update.
    /// </summary>
    public void AddRecentProject(string path, string name)
    {
        // Remove existing entry if present
        var existing = RecentProjects.FirstOrDefault(p => p.Path == path);
        if (existing != null)
            RecentProjects.Remove(existing);

        // Add to front of list
        RecentProjects.Insert(0, new RecentProject
        {
            Path = path,
            Name = name,
            LastOpened = DateTime.Now
        });

        // Trim to max size
        if (RecentProjects.Count > MaxRecentProjects)
            RecentProjects.RemoveRange(MaxRecentProjects, 
                                      RecentProjects.Count - MaxRecentProjects);

        Save();
    }

    /// <summary>
    /// Removes a project from the recent projects list (e.g., if deleted or invalid).
    /// Automatically saves preferences after update.
    /// </summary>
    public void RemoveRecentProject(string path)
    {
        RecentProjects.RemoveAll(p => p.Path == path);
        Save();
    }

    /// <summary>
    /// Saves preferences to disk in JSON format.
    /// </summary>
    public void Save()
    {
        try
        {
            var directory = System.IO.Path.GetDirectoryName(PreferencesPath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory!);

            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            File.WriteAllText(PreferencesPath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save editor preferences: {ex.Message}");
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
                return JsonSerializer.Deserialize<EditorPreferences>(json) 
                       ?? new EditorPreferences();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load editor preferences: {ex.Message}");
        }

        return new EditorPreferences();
    }
}
