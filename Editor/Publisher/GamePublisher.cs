using Engine.Core;
using Engine.Scripting;
using Serilog;

namespace Editor.Publisher;

public partial class GamePublisher(IProjectContext projectContext)
    : IGamePublisher
{
    private static readonly ILogger Logger = Log.ForContext<GamePublisher>();

    public void Publish()
    {
        var settings = new PublishSettings
        {
            OutputPath = GetDefaultOutputPath(),
            RuntimeIdentifier = PlatformDetection.DetectCurrentPlatform()
        };

        var result = PublishAsync(settings, CreateDefaultGameConfig()).GetAwaiter().GetResult();

        if (!result.Success)
            Logger.Error("Publish failed: {Error}", result.ErrorMessage);
    }

    public Task<PublishResult> PublishAsync(
        PublishSettings settings,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default) =>
        PublishAsync(settings, CreateDefaultGameConfig(), progress, cancellationToken);

    public async Task<PublishResult> PublishAsync(
        PublishSettings settings,
        GameConfiguration gameConfig,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var buildOutput = new List<string>();
        string? tempOutputPath = null;

        try
        {
            var validationResult = ValidateProject();
            if (!validationResult.Success)
                return validationResult;

            var settingsValidation = ValidateSettings(settings);
            if (!settingsValidation.Success)
                return settingsValidation;

            var startupSceneValidation = ValidateStartupScene(gameConfig);
            if (!startupSceneValidation.Success)
                return startupSceneValidation;

            if (projectContext.Root is null)
            {
                const string error = "No project directory available for asset packaging.";
                Logger.Error(error);
                return PublishResult.Failed(error);
            }

            var assetsDirValidation = PublishedAssetValidator.ValidateAssetsDirectory(projectContext.Root);
            if (!assetsDirValidation.Success)
                return assetsDirValidation;

            progress?.Report("Preparing build directory...");
            Logger.Information("Starting publish with settings: OutputPath={OutputPath}, Runtime={Runtime}",
                settings.OutputPath, settings.RuntimeIdentifier);

            var outputPath = string.IsNullOrWhiteSpace(settings.OutputPath)
                ? GetDefaultOutputPath()
                : settings.OutputPath;

            tempOutputPath = Path.Combine(Path.GetTempPath(), $"GameBuild_{Guid.NewGuid()}");

            try
            {
                Directory.CreateDirectory(tempOutputPath);
            }
            catch (Exception ex)
            {
                var error = $"Failed to create temporary build directory: {ex.Message}";
                Logger.Error(ex, "Failed to create temporary directory");
                return PublishResult.Failed(error);
            }

            ReportProgress(progress, "Building game runtime...", 0.1f);
            var buildResult = await BuildRuntimeAsync(settings, tempOutputPath, buildOutput, progress, cancellationToken);
            if (!buildResult.Success)
            {
                CleanupTempDirectory(tempOutputPath);
                return buildResult;
            }

            ReportProgress(progress, "Copying assets...", 0.5f);
            var copyAssetsResult = CopyAssets(tempOutputPath, settings);
            if (!copyAssetsResult.Success)
            {
                CleanupTempDirectory(tempOutputPath);
                return copyAssetsResult;
            }

            ReportProgress(progress, "Validating asset references...", 0.55f);
            var assetRefsValidation = PublishedAssetValidator.ValidateAssetReferences(
                Path.Combine(tempOutputPath, "assets"));
            if (!assetRefsValidation.Success)
            {
                Logger.Error(assetRefsValidation.ErrorMessage ?? "Asset reference validation failed");
                CleanupTempDirectory(tempOutputPath);
                return assetRefsValidation;
            }

            if (string.Equals(settings.Configuration, "Debug", StringComparison.OrdinalIgnoreCase))
            {
                ReportProgress(progress, "Copying scripts...", 0.7f);
                var copyScriptsResult = CopyScripts(tempOutputPath);
                if (!copyScriptsResult.Success)
                {
                    CleanupTempDirectory(tempOutputPath);
                    return copyScriptsResult;
                }
            }

            ReportProgress(progress, "Compiling game scripts to GameAssembly.dll...", 0.75f);
            var scriptsSource = projectContext.ScriptsDir!;
            var gameDllPath = Path.Combine(tempOutputPath, "GameAssembly.dll");
            if (!GameAssemblyCompiler.TryCompile(scriptsSource, gameDllPath, emitPdb: false, useDebugOptimization: false, out var scriptBuildErrors))
            {
                foreach (var line in scriptBuildErrors)
                {
                    buildOutput.Add(line);
                    Logger.Error("Script build: {Line}", line);
                }

                CleanupTempDirectory(tempOutputPath);
                return PublishResult.Failed("Compiling project scripts to GameAssembly.dll failed. See build output for Roslyn errors.");
            }

            ReportProgress(progress, "Creating game configuration...", 0.8f);
            var mergedConfig = MergeGameConfig(gameConfig);
            var configResult = CreateGameConfig(tempOutputPath, mergedConfig);
            if (!configResult.Success)
            {
                CleanupTempDirectory(tempOutputPath);
                return configResult;
            }

            ReportProgress(progress, "Validating build...", 0.9f);
            var validationCheck = PublishedBuildValidator.Validate(
                tempOutputPath, settings.RuntimeIdentifier, mergedConfig);
            if (!validationCheck.Success)
            {
                Logger.Error(validationCheck.ErrorMessage ?? "Published build validation failed");
                CleanupTempDirectory(tempOutputPath);
                return validationCheck;
            }

            Logger.Information("Published build validation passed");

            ReportProgress(progress, "Finalizing build...", 0.95f);
            var finalizeResult = FinalizeBuild(tempOutputPath, outputPath);
            if (!finalizeResult.Success)
            {
                CleanupTempDirectory(tempOutputPath);
                return finalizeResult;
            }

            tempOutputPath = null;

            ReportProgress(progress, "Publish completed successfully!", 1.0f);
            Logger.Information("Game published successfully to {OutputPath}", outputPath);

            return new PublishResult
            {
                Success = true,
                OutputPath = outputPath,
                BuildOutput = buildOutput
            };
        }
        catch (OperationCanceledException)
        {
            Logger.Warning("Publish operation was cancelled");
            CleanupTempDirectory(tempOutputPath);
            return new PublishResult
            {
                Success = false,
                ErrorMessage = "Publish operation was cancelled",
                BuildOutput = buildOutput
            };
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Unexpected error during publish");
            CleanupTempDirectory(tempOutputPath);
            return new PublishResult
            {
                Success = false,
                ErrorMessage = $"Unexpected error: {ex.Message}",
                BuildOutput = buildOutput
            };
        }
    }

    private static GameConfiguration CreateDefaultGameConfig(string title = "My Game") => new()
    {
        GameAssemblyPath = "GameAssembly.dll",
        StartupScenePath = "assets/scenes/Scene.scene",
        WindowWidth = 1920,
        WindowHeight = 1080,
        GameTitle = title
    };

    private static GameConfiguration MergeGameConfig(GameConfiguration gameConfig) => new()
    {
        GameAssemblyPath = string.IsNullOrWhiteSpace(gameConfig.GameAssemblyPath)
            ? "GameAssembly.dll"
            : gameConfig.GameAssemblyPath,
        StartupScenePath = gameConfig.StartupScenePath,
        WindowWidth = gameConfig.WindowWidth,
        WindowHeight = gameConfig.WindowHeight,
        Fullscreen = gameConfig.Fullscreen,
        GameTitle = gameConfig.GameTitle,
        TargetFrameRate = gameConfig.TargetFrameRate
    };

    private string GetDefaultOutputPath()
        => Path.Combine(projectContext.Root ?? Environment.CurrentDirectory, "Builds");

    /// <summary>
    /// Moves the temp build into the final output path. Creates the parent folder when missing
    /// and falls back to copy+delete when <see cref="Directory.Move"/> cannot rename across volumes.
    /// </summary>
    private static PublishResult FinalizeBuild(string tempOutputPath, string outputPath)
    {
        try
        {
            var parent = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(parent))
                Directory.CreateDirectory(parent);

            if (Directory.Exists(outputPath))
                Directory.Delete(outputPath, recursive: true);

            try
            {
                Directory.Move(tempOutputPath, outputPath);
            }
            catch (IOException)
            {
                // Cross-volume rename fails on macOS/Linux; copy then delete source.
                CopyDirectory(tempOutputPath, outputPath);
                Directory.Delete(tempOutputPath, recursive: true);
            }

            return new PublishResult { Success = true, OutputPath = outputPath };
        }
        catch (Exception ex)
        {
            var error = $"Failed to move build to output directory: {ex.Message}";
            Logger.Error(ex, "Failed to finalize build at {OutputPath}", outputPath);
            return PublishResult.Failed(error);
        }
    }

    private static void ReportProgress(IProgress<string>? progress, string message, float percentage)
    {
        progress?.Report(message);

        if (progress is PublishProgress publishProgress)
            publishProgress.SetProgress(percentage);
    }

    private static void CleanupTempDirectory(string? tempPath)
    {
        if (tempPath is null || !Directory.Exists(tempPath))
            return;

        try
        {
            Directory.Delete(tempPath, recursive: true);
            Logger.Debug("Cleaned up temporary directory: {Path}", tempPath);
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to clean up temporary directory: {Path}", tempPath);
        }
    }
}
