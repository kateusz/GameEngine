using System.Diagnostics;
using System.Text.Json;
using Editor.Features.Project;
using Serilog;

namespace Editor.Publisher;

public class GamePublisher(IProjectManager projectManager) : IGamePublisher
{
    private static readonly ILogger Logger = Log.ForContext<GamePublisher>();

    private static readonly HashSet<string> SupportedRuntimeIdentifiers = new(StringComparer.OrdinalIgnoreCase)
    {
        "win-x64", "win-x86", "win-arm64",
        "osx-x64", "osx-arm64",
        "linux-x64", "linux-arm64"
    };

    public void Publish()
    {
        var settings = new PublishSettings
        {
            OutputPath = GetDefaultOutputPath(),
            RuntimeIdentifier = PlatformDetection.DetectCurrentPlatform()
        };

        var gameConfig = new GameConfiguration
        {
            StartupScenePath = "assets/scenes/Scene.scene",
            WindowWidth = 1920,
            WindowHeight = 1080,
            Fullscreen = false,
            GameTitle = "My Game",
            TargetFrameRate = 60
        };

        var result = PublishAsync(settings, gameConfig).GetAwaiter().GetResult();

        if (!result.Success)
        {
            Logger.Error("Publish failed: {Error}", result.ErrorMessage);
        }
    }

    public Task<PublishResult> PublishAsync(
        PublishSettings settings,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var defaultGameConfig = new GameConfiguration
        {
            StartupScenePath = "assets/scenes/Scene.scene",
            WindowWidth = 1920,
            WindowHeight = 1080,
            Fullscreen = false,
            GameTitle = "My Game",
            TargetFrameRate = 60
        };

        return PublishAsync(settings, defaultGameConfig, progress, cancellationToken);
    }

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
            {
                return validationResult;
            }

            var settingsValidation = ValidateSettings(settings);
            if (!settingsValidation.Success)
            {
                return settingsValidation;
            }

            progress?.Report("Preparing build directory...");
            Logger.Information("Starting publish with settings: OutputPath={OutputPath}, Runtime={Runtime}",
                settings.OutputPath, settings.RuntimeIdentifier);

            var outputPath = string.IsNullOrWhiteSpace(settings.OutputPath)
                ? GetDefaultOutputPath()
                : settings.OutputPath;

            // Build to temporary directory for atomicity
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
            var copyAssetsResult = CopyAssets(tempOutputPath);
            if (!copyAssetsResult.Success)
            {
                CleanupTempDirectory(tempOutputPath);
                return copyAssetsResult;
            }

            ReportProgress(progress, "Copying scripts...", 0.7f);
            var copyScriptsResult = CopyScripts(tempOutputPath);
            if (!copyScriptsResult.Success)
            {
                CleanupTempDirectory(tempOutputPath);
                return copyScriptsResult;
            }

            ReportProgress(progress, "Creating game configuration...", 0.8f);
            var configResult = CreateGameConfig(tempOutputPath, gameConfig);
            if (!configResult.Success)
            {
                CleanupTempDirectory(tempOutputPath);
                return configResult;
            }

            ReportProgress(progress, "Validating build...", 0.9f);
            var validationCheck = ValidatePublishedBuild(tempOutputPath, settings.RuntimeIdentifier, gameConfig);
            if (!validationCheck.Success)
            {
                CleanupTempDirectory(tempOutputPath);
                return validationCheck;
            }

            // Move from temp to final location (atomic operation)
            ReportProgress(progress, "Finalizing build...", 0.95f);
            try
            {
                if (Directory.Exists(outputPath))
                {
                    Directory.Delete(outputPath, recursive: true);
                }

                Directory.Move(tempOutputPath, outputPath);
                tempOutputPath = null; // Prevent cleanup
            }
            catch (Exception ex)
            {
                var error = $"Failed to move build to output directory: {ex.Message}";
                Logger.Error(ex, "Failed to move build");
                CleanupTempDirectory(tempOutputPath);
                return PublishResult.Failed(error);
            }

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

    private static void ReportProgress(IProgress<string>? progress, string message, float percentage)
    {
        progress?.Report(message);

        if (progress is PublishProgress publishProgress)
        {
            publishProgress.SetProgress(percentage);
        }
    }

    private static void CleanupTempDirectory(string? tempPath)
    {
        if (tempPath != null && Directory.Exists(tempPath))
        {
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

    private PublishResult ValidateProject()
    {
        if (projectManager.ScriptsDir is null || projectManager.ScenesDir is null)
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

    private string GetDefaultOutputPath()
        => Path.Combine(projectManager.CurrentProjectDirectory ?? Environment.CurrentDirectory, "Builds");

    private async Task<PublishResult> BuildRuntimeAsync(
        PublishSettings settings,
        string outputPath,
        List<string> buildOutput,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        var runtimeProjectPath = FindRuntimeProject();
        if (runtimeProjectPath is null)
        {
            const string error = "Could not find Runtime.csproj. Ensure the Runtime project exists in the solution.";
            Logger.Error(error);
            return PublishResult.Failed(error);
        }

        var arguments = BuildDotnetPublishArguments(settings, runtimeProjectPath, outputPath);

        Logger.Information("Running: dotnet {Arguments}", arguments);
        progress?.Report($"Compiling runtime executable...");

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using var process = new Process { StartInfo = psi };

            process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    buildOutput.Add(e.Data);
                    Logger.Debug("{BuildOutput}", e.Data);
                    progress?.Report(e.Data);
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    buildOutput.Add($"ERROR: {e.Data}");
                    Logger.Error("Build error: {ErrorData}", e.Data);
                    progress?.Report($"ERROR: {e.Data}");
                }
            };

            if (!process.Start())
            {
                return PublishResult.Failed("Failed to start dotnet process.");
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                var error = $"Build failed with exit code {process.ExitCode}. Check build output for details.";
                Logger.Error(error);
                return new PublishResult
                {
                    Success = false,
                    ErrorMessage = error,
                    BuildOutput = buildOutput
                };
            }

            return new PublishResult { Success = true, BuildOutput = buildOutput };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var error = $"Failed to execute dotnet publish: {ex.Message}";
            Logger.Error(ex, "dotnet publish execution failed");
            return new PublishResult
            {
                Success = false,
                ErrorMessage = error,
                BuildOutput = buildOutput
            };
        }
    }

    private static string BuildDotnetPublishArguments(PublishSettings settings, string projectPath, string outputPath)
    {
        var args = $"publish \"{projectPath}\" " +
                   $"-c {settings.Configuration} " +
                   $"-r {settings.RuntimeIdentifier} " +
                   $"-o \"{outputPath}\" " +
                   $"--self-contained {settings.SelfContained.ToString().ToLowerInvariant()}";

        if (settings.SingleFile)
        {
            args += " /p:PublishSingleFile=true";
        }

        return args;
    }

    private string? FindRuntimeProject()
    {
        // First, try to find the solution file
        var solutionPath = FindSolutionFile();
        if (solutionPath != null)
        {
            var solutionDir = Path.GetDirectoryName(solutionPath)!;
            var runtimeCsproj = Path.Combine(solutionDir, "Runtime", "Runtime.csproj");

            if (File.Exists(runtimeCsproj))
            {
                Logger.Debug("Found Runtime project at {Path} (via solution file)", runtimeCsproj);
                return runtimeCsproj;
            }
        }

        // Fallback: try relative paths from current directory
        var possiblePaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Runtime", "Runtime.csproj"),
            Path.Combine(Environment.CurrentDirectory, "Runtime", "Runtime.csproj"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Runtime", "Runtime.csproj"))
        };

        foreach (var path in possiblePaths)
        {
            var fullPath = Path.GetFullPath(path);
            if (File.Exists(fullPath))
            {
                Logger.Debug("Found Runtime project at {Path} (via relative path)", fullPath);
                return fullPath;
            }
        }

        Logger.Error("Could not find Runtime.csproj in any known location");
        return null;
    }

    private string? FindSolutionFile()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir != null)
        {
            var sln = dir.GetFiles("*.sln").FirstOrDefault();
            if (sln != null)
            {
                Logger.Debug("Found solution file: {Path}", sln.FullName);
                return sln.FullName;
            }

            dir = dir.Parent;
        }

        Logger.Warning("Could not find .sln file");
        return null;
    }

    private PublishResult CopyAssets(string buildOutput)
    {
        if (projectManager.CurrentProjectDirectory is null)
        {
            Logger.Warning("No project directory available for asset copying");
            return new PublishResult { Success = true };
        }

        var assetsSource = Path.Combine(projectManager.CurrentProjectDirectory, "assets");
        if (!Directory.Exists(assetsSource))
        {
            Logger.Information("No assets directory found at {Path}, skipping asset copy", assetsSource);
            return new PublishResult { Success = true };
        }

        var assetsTarget = Path.Combine(buildOutput, "assets");

        try
        {
            CopyDirectory(assetsSource, assetsTarget);
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
        var scriptsSource = projectManager.ScriptsDir;
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
            var json = JsonSerializer.Serialize(gameConfig, new JsonSerializerOptions
            {
                WriteIndented = true
            });

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

    private static PublishResult ValidatePublishedBuild(
        string outputPath,
        string runtimeIdentifier,
        GameConfiguration gameConfig)
    {
        var exeName = PlatformDetection.GetExecutableName(runtimeIdentifier);
        var exePath = Path.Combine(outputPath, exeName);

        if (!File.Exists(exePath))
        {
            var error = $"Published executable not found at {exePath}";
            Logger.Error(error);
            return PublishResult.Failed(error);
        }

        var exeInfo = new FileInfo(exePath);
        if (exeInfo.Length < 1024 * 100)
        {
            var error = $"Executable suspiciously small: {exeInfo.Length} bytes";
            Logger.Warning(error);
        }

        var configPath = Path.Combine(outputPath, "game.config.json");
        if (!File.Exists(configPath))
        {
            var error = $"Game configuration not found at {configPath}";
            Logger.Error(error);
            return PublishResult.Failed(error);
        }

        var startupScenePath = Path.Combine(outputPath, gameConfig.StartupScenePath);
        if (!File.Exists(startupScenePath))
        {
            var error = $"Startup scene not found: {startupScenePath}";
            Logger.Warning(error);
        }

        Logger.Information("Published build validation passed");
        return PublishResult.Succeeded("Validation passed");
    }

    private static void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, file);
            var destPath = Path.Combine(targetDir, relativePath);
            var destDirectory = Path.GetDirectoryName(destPath);

            if (!string.IsNullOrEmpty(destDirectory))
            {
                Directory.CreateDirectory(destDirectory);
            }

            File.Copy(file, destPath, overwrite: true);
        }
    }
}
