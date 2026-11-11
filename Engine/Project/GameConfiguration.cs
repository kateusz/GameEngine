using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Engine.Project;

public class GameConfiguration
{
    public const string FileName = "game.config.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public string GameAssemblyPath { get; set; } = "GameAssembly.dll";
    public string StartupScenePath { get; set; } = "assets/scenes/game.scene";
    public int WindowWidth { get; set; } = 1920;
    public int WindowHeight { get; set; } = 1080;
    public bool Fullscreen { get; set; }
    public string GameTitle { get; set; } = "My Game";
    public int TargetFrameRate { get; set; } = 60;

    public static string PathFor(string directory) => Path.Combine(directory, FileName);

    public static GameConfiguration ForNewProject(string title, string startupScenePath) => new()
    {
        GameTitle = title,
        StartupScenePath = startupScenePath
    };

    public static bool TryLoad(
        string path,
        [NotNullWhen(true)] out GameConfiguration? config,
        [NotNullWhen(false)] out string? error)
    {
        config = null;
        error = null;

        if (!File.Exists(path))
        {
            error = $"Game configuration not found at {path}";
            return false;
        }

        try
        {
            var json = File.ReadAllText(path);
            config = JsonSerializer.Deserialize<GameConfiguration>(json);
            if (config is null)
            {
                error = $"Failed to deserialize game configuration at {path}";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            error = $"Failed to load game configuration at {path}: {ex.Message}";
            return false;
        }
    }

    public static void Save(string path, GameConfiguration config)
    {
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(path, json);
    }

    public static string NormalizeSceneRelativePath(string path) =>
        path.Replace('\\', '/').Trim();

    public static bool TryResolveStartupSceneFile(
        string projectRoot,
        string startupScenePath,
        [NotNullWhen(true)] out string? absolutePath)
    {
        absolutePath = null;
        if (string.IsNullOrWhiteSpace(startupScenePath))
            return false;

        if (!Directory.Exists(projectRoot))
            return false;

        var normalized = NormalizeSceneRelativePath(startupScenePath);
        foreach (var file in Directory.EnumerateFiles(projectRoot, "*.scene", SearchOption.AllDirectories))
        {
            var relative = NormalizeSceneRelativePath(Path.GetRelativePath(projectRoot, file));
            if (string.Equals(relative, normalized, StringComparison.OrdinalIgnoreCase))
            {
                absolutePath = Path.GetFullPath(file);
                return true;
            }
        }

        return false;
    }

    public static string? CanonicalStartupScenePath(string projectRoot, string startupScenePath)
    {
        if (!TryResolveStartupSceneFile(projectRoot, startupScenePath, out var absolute))
            return null;

        return NormalizeSceneRelativePath(Path.GetRelativePath(projectRoot, absolute));
    }

    public static string? FirstSceneRelativePath(string projectRoot)
    {
        var scenesDir = Path.Combine(projectRoot, "assets", "scenes");
        if (!Directory.Exists(scenesDir))
            return null;

        return Directory.EnumerateFiles(scenesDir, "*.scene", SearchOption.AllDirectories)
            .Select(file => NormalizeSceneRelativePath(Path.GetRelativePath(projectRoot, file)))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }
}
