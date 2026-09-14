using Editor.Scripting;
using Engine.Core;
using Serilog;

namespace Editor.Publisher;

public partial class GamePublisher(IProjectContext projectContext)
    : IGamePublisher
{
    private static readonly ILogger Logger = Log.ForContext<GamePublisher>();

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

            var renameResult = RenamePublishedExecutable(tempOutputPath, settings.RuntimeIdentifier, gameConfig.GameTitle);
            if (!renameResult.Success)
            {
                CleanupTempDirectory(tempOutputPath);
                return renameResult;
            }

            ReportProgress(progress, "Copying assets...", 0.5f);
            var copyAssetsResult = CopyAssets(tempOutputPath);
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
            var configResult = CreateGameConfig(tempOutputPath, gameConfig);
            if (!configResult.Success)
            {
                CleanupTempDirectory(tempOutputPath);
                return configResult;
            }

            ReportProgress(progress, "Validating build...", 0.9f);
            var validationCheck = PublishedBuildValidator.Validate(
                tempOutputPath, settings.RuntimeIdentifier, gameConfig);
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

    private string GetDefaultOutputPath()
        => Path.Combine(projectContext.Root ?? Environment.CurrentDirectory, "Builds");

    private static PublishResult RenamePublishedExecutable(string outputPath, string runtimeIdentifier, string gameTitle)
    {
        var produced = Path.Combine(outputPath, PlatformDetection.GetExecutableName(runtimeIdentifier));
        var shipped = Path.Combine(outputPath, PlatformDetection.GetPublishedExecutableName(runtimeIdentifier, gameTitle));

        if (string.Equals(produced, shipped, StringComparison.OrdinalIgnoreCase))
            return new PublishResult { Success = true };

        if (!File.Exists(produced))
            return PublishResult.Failed($"Published executable not found at {produced}");

        try
        {
            if (File.Exists(shipped))
                File.Delete(shipped);
            File.Move(produced, shipped);
            Logger.Information("Renamed published executable to {Path}", shipped);
            return new PublishResult { Success = true };
        }
        catch (Exception ex)
        {
            var error = $"Failed to rename published executable to {shipped}: {ex.Message}";
            Logger.Error(ex, "Failed to rename published executable");
            return PublishResult.Failed(error);
        }
    }

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
