using Engine.Core;

namespace Editor.Publisher;

public partial class GamePublisher
{
    private PublishResult CopyAssets(string buildOutput)
    {
        var assetsSource = Path.Combine(projectContext.Root!, "assets");
        var assetsTarget = Path.Combine(buildOutput, "assets");

        try
        {
            CopyDirectory(assetsSource, assetsTarget);
            Logger.Information("Copied assets from {Source} to {Target}", assetsSource, assetsTarget);
            return PublishResult.Ok();
        }
        catch (Exception ex)
        {
            var error = $"Failed to copy assets: {ex.Message}";
            Logger.Error(ex, "Failed to copy assets from {Source} to {Target}", assetsSource, assetsTarget);
            return PublishResult.Failed(error);
        }
    }

    private static PublishResult CreateGameConfig(string buildOutput, GameConfiguration gameConfig)
    {
        try
        {
            var configPath = GameConfiguration.PathFor(buildOutput);
            GameConfiguration.Save(configPath, gameConfig);
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

    private static void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, file);
            if (IsUnderScriptsFolder(relativePath))
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
