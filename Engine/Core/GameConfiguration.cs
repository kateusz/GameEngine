using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Engine.Core;

public class GameConfiguration
{
    public const string FileName = "game.config.json";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

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
}
