using Engine.Core;

namespace Editor.Publisher;

public partial class GamePublisher
{
    private static readonly HashSet<string> SupportedRuntimeIdentifiers = new(StringComparer.OrdinalIgnoreCase)
    {
        "win-x64", "win-x86", "win-arm64",
        "osx-x64", "osx-arm64"
    };

    private PublishResult ValidateProject()
    {
        if (projectContext.ScriptsDir is null || projectContext.ScenesDir is null)
        {
            const string error = "No project is currently loaded. Please open a project before publishing.";
            Logger.Warning(error);
            return PublishResult.Failed(error);
        }

        return new PublishResult { Success = true };
    }

    private static PublishResult ValidateSettings(PublishSettings settings)
    {
        if (!SupportedRuntimeIdentifiers.Contains(settings.RuntimeIdentifier))
        {
            var error = $"Unsupported runtime identifier '{settings.RuntimeIdentifier}'. " +
                        $"Supported values: {string.Join(", ", SupportedRuntimeIdentifiers)}";
            Logger.Warning(error);
            return PublishResult.Failed(error);
        }

        if (string.IsNullOrWhiteSpace(settings.Configuration))
            return PublishResult.Failed("Build configuration cannot be empty.");

        return new PublishResult { Success = true };
    }

    private PublishResult ValidateStartupScene(GameConfiguration gameConfig)
    {
        var startupScenePath = Path.Combine(projectContext.Root!, gameConfig.StartupScenePath);
        if (File.Exists(startupScenePath))
            return new PublishResult { Success = true };

        var error = $"Startup scene not found: {startupScenePath}";
        Logger.Error(error);
        return PublishResult.Failed(error);
    }
}
