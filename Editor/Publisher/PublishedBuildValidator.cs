using Engine.Core;

namespace Editor.Publisher;

/// <summary>
/// Post-publish checks for required package artifacts.
/// </summary>
public static class PublishedBuildValidator
{
    public const long MinimumExecutableBytes = 100 * 1024;

    public static PublishResult Validate(
        string outputPath,
        string runtimeIdentifier,
        GameConfiguration gameConfig)
    {
        var exeName = PlatformDetection.GetExecutableName(runtimeIdentifier);
        var exePath = Path.Combine(outputPath, exeName);

        if (!File.Exists(exePath))
            return PublishResult.Failed($"Published executable not found at {exePath}");

        var exeInfo = new FileInfo(exePath);
        if (exeInfo.Length < MinimumExecutableBytes)
        {
            return PublishResult.Failed(
                $"Published executable is suspiciously small ({exeInfo.Length} bytes). " +
                $"Expected at least {MinimumExecutableBytes} bytes — the Runtime publish likely failed.");
        }

        var configPath = Path.Combine(outputPath, "game.config.json");
        if (!File.Exists(configPath))
            return PublishResult.Failed($"Game configuration not found at {configPath}");

        var relDll = string.IsNullOrWhiteSpace(gameConfig.GameAssemblyPath)
            ? "GameAssembly.dll"
            : gameConfig.GameAssemblyPath;
        var gameAssemblyPath = Path.Combine(outputPath, relDll);
        if (!File.Exists(gameAssemblyPath))
            return PublishResult.Failed($"GameAssembly not found at {gameAssemblyPath}");

        var startupScenePath = Path.Combine(outputPath, gameConfig.StartupScenePath);
        if (!File.Exists(startupScenePath))
            return PublishResult.Failed($"Startup scene not found: {startupScenePath}");

        return PublishResult.Succeeded("Validation passed");
    }
}
