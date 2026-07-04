using System.Text.Json;
using Engine.Core;

namespace Editor.Publisher;

public partial class GamePublisher
{
    private PublishResult CopyAssets(string buildOutput, PublishSettings settings)
    {
        if (projectContext.Root is null)
        {
            Logger.Warning("No project directory available for asset copying");
            return new PublishResult { Success = true };
        }

        var assetsSource = Path.Combine(projectContext.Root, "assets");
        if (!Directory.Exists(assetsSource))
        {
            Logger.Information("No assets directory found at {Path}, skipping asset copy", assetsSource);
            return new PublishResult { Success = true };
        }

        var assetsTarget = Path.Combine(buildOutput, "assets");
        var includeScripts = string.Equals(settings.Configuration, "Debug", StringComparison.OrdinalIgnoreCase);

        try
        {
            CopyDirectory(assetsSource, assetsTarget, includeScripts);
            Logger.Information("Copied assets from {Source} to {Target}", assetsSource, assetsTarget);
            return new PublishResult { Success = true };
        }
        catch (Exception ex)
        {
            var error = $"Failed to copy assets: {ex.Message}";
            Logger.Error(ex, "Failed to copy assets from {Source} to {Target}", assetsSource, assetsTarget);
            return PublishResult.Failed(error);
        }
    }

    private PublishResult CopyScripts(string buildOutput)
    {
        var scriptsSource = projectContext.ScriptsDir;
        if (scriptsSource is null || !Directory.Exists(scriptsSource))
        {
            Logger.Information("No scripts directory found, skipping script copy");
            return new PublishResult { Success = true };
        }

        var scriptsTarget = Path.Combine(buildOutput, "assets", "scripts");

        try
        {
            CopyDirectory(scriptsSource, scriptsTarget);
            Logger.Information("Copied scripts from {Source} to {Target}", scriptsSource, scriptsTarget);
            return new PublishResult { Success = true };
        }
        catch (Exception ex)
        {
            var error = $"Failed to copy scripts: {ex.Message}";
            Logger.Error(ex, "Failed to copy scripts from {Source} to {Target}", scriptsSource, scriptsTarget);
            return PublishResult.Failed(error);
        }
    }

    private static PublishResult CreateGameConfig(string buildOutput, GameConfiguration gameConfig)
    {
        try
        {
            var configPath = Path.Combine(buildOutput, "game.config.json");
            var json = JsonSerializer.Serialize(gameConfig, new JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(configPath, json);
            Logger.Information("Created game configuration at {Path}", configPath);
            return PublishResult.Succeeded(configPath);
        }
        catch (Exception ex)
        {
            var error = $"Failed to create game configuration: {ex.Message}";
            Logger.Error(ex, "Failed to create game.config.json");
            return PublishResult.Failed(error);
        }
    }

    private static void CopyDirectory(string sourceDir, string targetDir, bool includeScripts = true)
    {
        Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, file);
            if (!includeScripts && IsUnderScriptsFolder(relativePath))
                continue;

            var destPath = Path.Combine(targetDir, relativePath);
            var destDirectory = Path.GetDirectoryName(destPath);

            if (!string.IsNullOrEmpty(destDirectory))
                Directory.CreateDirectory(destDirectory);

            File.Copy(file, destPath, overwrite: true);
        }
    }

    private static bool IsUnderScriptsFolder(string relativePath)
    {
        var normalized = relativePath.Replace('\\', '/');
        return normalized.StartsWith("scripts/", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "scripts", StringComparison.OrdinalIgnoreCase);
    }
}
