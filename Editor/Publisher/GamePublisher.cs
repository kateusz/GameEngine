using System.Diagnostics;
using Editor.Features.Project;
using Serilog;

namespace Editor.Publisher;

/// <summary>
/// Publishes game projects by building the runtime and copying assets/scripts.
/// </summary>
public class GamePublisher(IProjectManager projectManager) : IGamePublisher
{
    private static readonly ILogger Logger = Log.ForContext<GamePublisher>();
    
    private static readonly HashSet<string> SupportedRuntimeIdentifiers = new(StringComparer.OrdinalIgnoreCase)
    {
        "win-x64", "win-x86", "win-arm64",
        "osx-x64", "osx-arm64",
        "linux-x64", "linux-arm64"
    };

    /// <inheritdoc />
    public void Publish()
    {
        // Legacy synchronous method - delegates to async version with default settings
        // Note: Using GetAwaiter().GetResult() instead of Wait() to properly propagate exceptions
        var settings = new PublishSettings
        {
            OutputPath = GetDefaultOutputPath()
        };
        
        var result = PublishAsync(settings).GetAwaiter().GetResult();
        
        if (!result.Success)
        {
            Logger.Error("Publish failed: {Error}", result.ErrorMessage);
        }
    }

    /// <inheritdoc />
    public async Task<PublishResult> PublishAsync(
        PublishSettings settings, 
        IProgress<string>? progress = null, 
        CancellationToken cancellationToken = default)
    {
        var buildOutput = new List<string>();
        
        try
        {
            // Validate project is loaded
            var validationResult = ValidateProject();
            if (!validationResult.Success)
            {
                return validationResult;
            }

            // Validate settings
            var settingsValidation = ValidateSettings(settings);
            if (!settingsValidation.Success)
            {
                return settingsValidation;
            }

            progress?.Report("Preparing build directory...");
            Logger.Information("Starting publish with settings: OutputPath={OutputPath}, Runtime={Runtime}", 
                settings.OutputPath, settings.RuntimeIdentifier);

            // Create output directory
            var outputPath = string.IsNullOrWhiteSpace(settings.OutputPath) 
                ? GetDefaultOutputPath() 
                : settings.OutputPath;
            
            try
            {
                Directory.CreateDirectory(outputPath);
            }
            catch (Exception ex)
            {
                var error = $"Failed to create output directory '{outputPath}': {ex.Message}";
                Logger.Error(ex, "Failed to create output directory");
                return PublishResult.Failed(error);
            }

            // Build the runtime
            progress?.Report("Building game runtime...");
            var buildResult = await BuildRuntimeAsync(settings, buildOutput, progress, cancellationToken);
            if (!buildResult.Success)
            {
                return buildResult;
            }

            // Copy assets
            progress?.Report("Copying assets...");
            var copyAssetsResult = CopyAssets(outputPath);
            if (!copyAssetsResult.Success)
            {
                return copyAssetsResult;
            }

            // Copy scripts
            progress?.Report("Copying scripts...");
            var copyScriptsResult = CopyScripts(outputPath);
            if (!copyScriptsResult.Success)
            {
                return copyScriptsResult;
            }

            progress?.Report("Publish completed successfully!");
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
            return new PublishResult
            {
                Success = false,
                ErrorMessage = $"Unexpected error: {ex.Message}",
                BuildOutput = buildOutput
            };
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

    private PublishResult ValidateSettings(PublishSettings settings)
    {
        if (!SupportedRuntimeIdentifiers.Contains(settings.RuntimeIdentifier))
        {
            var error = $"Unsupported runtime identifier '{settings.RuntimeIdentifier}'. " +
                        $"Supported values: {string.Join(", ", SupportedRuntimeIdentifiers)}";
            Logger.Warning(error);
            return PublishResult.Failed(error);
        }
        
        if (string.IsNullOrWhiteSpace(settings.Configuration))
        {
            return PublishResult.Failed("Build configuration cannot be empty.");
        }
        
        return new PublishResult { Success = true };
    }

    private string GetDefaultOutputPath()
    {
        // Use project-relative Builds directory if a project is loaded
        if (projectManager.CurrentProjectDirectory is not null)
        {
            return Path.Combine(projectManager.CurrentProjectDirectory, "Builds");
        }
        
        // Fallback to current directory
        return Path.Combine(Environment.CurrentDirectory, "Builds");
    }

    private async Task<PublishResult> BuildRuntimeAsync(
        PublishSettings settings,
        List<string> buildOutput,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        // Find the Runtime project - look relative to the project structure
        var runtimeProjectPath = FindRuntimeProject();
        if (runtimeProjectPath is null)
        {
            const string error = "Could not find Runtime.csproj. Ensure the Runtime project exists in the solution.";
            Logger.Error(error);
            return PublishResult.Failed(error);
        }

        var outputPath = string.IsNullOrWhiteSpace(settings.OutputPath) 
            ? GetDefaultOutputPath() 
            : settings.OutputPath;

        var arguments = BuildDotnetPublishArguments(settings, runtimeProjectPath, outputPath);
        
        Logger.Information("Running: dotnet {Arguments}", arguments);
        progress?.Report($"Running: dotnet {arguments}");

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
                    Logger.Information("{BuildOutput}", e.Data);
                    progress?.Report(e.Data);
                }
            };
            
            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    buildOutput.Add($"ERROR: {e.Data}");
                    Logger.Error("Build error: {ErrorData}", e.Data);
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
        // Try to find Runtime.csproj relative to the solution
        var possiblePaths = new[]
        {
            // Relative to the editor's base directory
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Runtime", "Runtime.csproj"),
            // Relative to the current working directory
            Path.Combine(Environment.CurrentDirectory, "Runtime", "Runtime.csproj"),
            // Sibling to Editor in solution structure
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Runtime", "Runtime.csproj"))
        };

        foreach (var path in possiblePaths)
        {
            var fullPath = Path.GetFullPath(path);
            if (File.Exists(fullPath))
            {
                Logger.Debug("Found Runtime project at {Path}", fullPath);
                return fullPath;
            }
        }

        return null;
    }

    private PublishResult CopyAssets(string buildOutput)
    {
        // Get assets source from the project manager
        if (projectManager.CurrentProjectDirectory is null)
        {
            Logger.Warning("No project directory available for asset copying");
            return new PublishResult { Success = true }; // Not a failure, just nothing to copy
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