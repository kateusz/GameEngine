using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Serilog;

namespace Editor.Publisher;

public class GamePublisher : IGamePublisher
{
    private static readonly Serilog.ILogger Logger = Log.ForContext<GamePublisher>();

    private BuildSettings _settings;
    private string _projectPath;
    private StringBuilder _buildLog = new();

    public event Action<string>? OnBuildProgress;
    public event Action<BuildReport>? OnBuildComplete;

    public GamePublisher()
    {
        _settings = new BuildSettings();
        _projectPath = AppContext.BaseDirectory;
    }

    public void SetProjectPath(string projectPath)
    {
        _projectPath = projectPath;
    }

    public void SetBuildSettings(BuildSettings settings)
    {
        _settings = settings;
    }

    public void Publish()
    {
        PublishAsync(_settings).Wait();
    }

    public async Task<BuildReport> PublishAsync(BuildSettings settings)
    {
        var report = new BuildReport
        {
            BuildTime = DateTime.Now,
            Settings = settings
        };

        var stopwatch = Stopwatch.StartNew();

        try
        {
            Logger.Information("=== Starting Build Process ===");
            LogProgress("Starting build process...");

            // 1. Validate settings
            LogProgress("Validating build settings...");
            if (!ValidateSettings(settings, out var validationErrors))
            {
                report.Success = false;
                report.Errors.AddRange(validationErrors);
                return report;
            }

            // 2. Prepare output directory
            var buildOutput = PrepareOutputDirectory(settings);
            report.OutputDirectory = buildOutput;
            LogProgress($"Output directory: {buildOutput}");

            // 3. Compile scripts (if enabled)
            if (settings.PrecompileScripts)
            {
                LogProgress("Compiling game scripts...");
                var (success, scriptDll, errors) = await CompileGameScriptsAsync(settings, buildOutput);
                if (!success)
                {
                    report.Success = false;
                    report.Errors.AddRange(errors);
                    return report;
                }
                report.CompiledScripts = true;
                report.Files.Add($"GameScripts.dll ({GetFileSize(scriptDll)} KB)");
            }

            // 4. Generate Runtime config
            LogProgress("Generating runtime configuration...");
            GenerateRuntimeConfig(settings, buildOutput);

            // 5. Build Runtime executable
            LogProgress($"Building runtime for {settings.Platform}...");
            var (buildSuccess, exePath, buildErrors) = await BuildRuntimeAsync(settings, buildOutput);
            if (!buildSuccess)
            {
                report.Success = false;
                report.Errors.AddRange(buildErrors);
                return report;
            }
            report.Files.Add($"{settings.GameName}{settings.GetExecutableExtension()} ({GetFileSize(exePath)} KB)");

            // 6. Copy assets
            LogProgress("Copying game assets...");
            var (assetCount, totalSize) = CopyAssets(settings, buildOutput);
            report.AssetCount = assetCount;
            report.Files.Add($"GameData/ ({totalSize} KB, {assetCount} files)");

            // 7. Generate build info
            GenerateBuildInfo(settings, buildOutput, report);

            stopwatch.Stop();
            report.Success = true;
            report.BuildDuration = stopwatch.Elapsed;
            report.BuildLog = _buildLog.ToString();

            Logger.Information("=== Build Complete ===");
            Logger.Information("Output: {OutputDirectory}", buildOutput);
            Logger.Information("Duration: {Duration:F2}s", report.BuildDuration.TotalSeconds);

            OnBuildComplete?.Invoke(report);
            return report;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Build failed with exception");
            report.Success = false;
            report.Errors.Add($"Build exception: {ex.Message}");
            report.BuildDuration = stopwatch.Elapsed;
            return report;
        }
    }

    private bool ValidateSettings(BuildSettings settings, out List<string> errors)
    {
        errors = new List<string>();

        // Validate startup scene
        if (string.IsNullOrWhiteSpace(settings.StartupScene))
        {
            errors.Add("Startup scene is not set");
        }
        else
        {
            var scenePath = ResolveScenePath(settings.StartupScene);
            if (scenePath == null || !File.Exists(scenePath))
            {
                errors.Add($"Startup scene not found: {settings.StartupScene}");
            }
        }

        // Validate game name
        if (string.IsNullOrWhiteSpace(settings.GameName))
        {
            errors.Add("Game name cannot be empty");
        }

        // Validate Runtime project exists
        var runtimeProjectPath = Path.Combine(_projectPath, "..", "Runtime", "Runtime.csproj");
        if (!File.Exists(runtimeProjectPath))
        {
            errors.Add($"Runtime project not found at: {runtimeProjectPath}");
        }

        return errors.Count == 0;
    }

    private string PrepareOutputDirectory(BuildSettings settings)
    {
        var outputDir = settings.OutputDirectory;
        if (!Path.IsPathRooted(outputDir))
        {
            outputDir = Path.Combine(_projectPath, "..", settings.OutputDirectory);
        }

        var buildOutput = Path.Combine(outputDir, settings.GameName);
        buildOutput = Path.GetFullPath(buildOutput);

        // Clean and create directory
        if (Directory.Exists(buildOutput))
        {
            Logger.Information("Cleaning existing build directory...");
            try
            {
                Directory.Delete(buildOutput, true);
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Could not clean build directory");
            }
        }

        Directory.CreateDirectory(buildOutput);
        Logger.Information("Build output: {BuildOutput}", buildOutput);

        return buildOutput;
    }

    private async Task<(bool Success, string DllPath, List<string> Errors)> CompileGameScriptsAsync(
        BuildSettings settings, string buildOutput)
    {
        var errors = new List<string>();

        try
        {
            // Find scripts directory
            var scriptsPath = Path.Combine(_projectPath, "assets", "scripts");
            if (!Directory.Exists(scriptsPath))
            {
                Logger.Warning("No scripts directory found at: {ScriptsPath}", scriptsPath);
                return (true, "", errors); // Not an error if there are no scripts
            }

            var scriptFiles = Directory.GetFiles(scriptsPath, "*.cs");
            if (scriptFiles.Length == 0)
            {
                Logger.Information("No scripts to compile");
                return (true, "", errors);
            }

            Logger.Information("Compiling {ScriptCount} scripts...", scriptFiles.Length);

            // Parse all scripts
            var syntaxTrees = new List<SyntaxTree>();
            foreach (var scriptFile in scriptFiles)
            {
                var source = await File.ReadAllTextAsync(scriptFile);
                var tree = CSharpSyntaxTree.ParseText(source, path: scriptFile,
                    encoding: Encoding.UTF8);
                syntaxTrees.Add(tree);
            }

            // Get references
            var references = GetCompilationReferences();

            // Create compilation
            var compilationOptions = new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: settings.Configuration == BuildConfiguration.Release
                    ? OptimizationLevel.Release
                    : OptimizationLevel.Debug,
                allowUnsafe: true,
                platform: Microsoft.CodeAnalysis.Platform.AnyCpu);

            var compilation = CSharpCompilation.Create(
                "GameScripts",
                syntaxTrees,
                references,
                compilationOptions);

            // Check for compilation errors before emitting
            var diagnostics = compilation.GetDiagnostics();
            var compilationErrors = diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToList();

            if (compilationErrors.Any())
            {
                foreach (var error in compilationErrors)
                {
                    var errorMsg = $"{error.Location.GetLineSpan().Path}({error.Location.GetLineSpan().StartLinePosition.Line + 1}): {error.GetMessage()}";
                    Logger.Error("Script compilation error: {Error}", errorMsg);
                    errors.Add(errorMsg);
                }
                return (false, "", errors);
            }

            // Emit to file
            var dllPath = Path.Combine(buildOutput, "GameScripts.dll");
            var pdbPath = Path.Combine(buildOutput, "GameScripts.pdb");

            using var dllStream = File.Create(dllPath);
            using var pdbStream = File.Create(pdbPath);

            var emitResult = compilation.Emit(dllStream, pdbStream);

            if (!emitResult.Success)
            {
                foreach (var diagnostic in emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
                {
                    var errorMsg = diagnostic.GetMessage();
                    Logger.Error("Emit error: {Error}", errorMsg);
                    errors.Add(errorMsg);
                }
                return (false, "", errors);
            }

            Logger.Information("Scripts compiled successfully: {DllPath}", dllPath);
            return (true, dllPath, errors);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Exception during script compilation");
            errors.Add($"Script compilation exception: {ex.Message}");
            return (false, "", errors);
        }
    }

    private MetadataReference[] GetCompilationReferences()
    {
        var references = new List<MetadataReference>();

        // .NET runtime assemblies
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location);
        var coreAssemblies = new[]
        {
            "System.Private.CoreLib.dll",
            "System.Runtime.dll",
            "System.Collections.dll",
            "System.Console.dll",
            "System.Linq.dll",
            "System.Numerics.dll",
            "System.Numerics.Vectors.dll"
        };

        foreach (var assembly in coreAssemblies)
        {
            var path = Path.Combine(runtimeDir!, assembly);
            if (File.Exists(path))
            {
                references.Add(MetadataReference.CreateFromFile(path));
            }
        }

        // Engine assemblies
        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Where(a => a.GetName().Name?.StartsWith("Engine") == true ||
                       a.GetName().Name?.StartsWith("ECS") == true);

        foreach (var assembly in loadedAssemblies)
        {
            references.Add(MetadataReference.CreateFromFile(assembly.Location));
        }

        return references.ToArray();
    }

    private void GenerateRuntimeConfig(BuildSettings settings, string buildOutput)
    {
        var config = new
        {
            StartupScene = settings.StartupScene,
            WindowTitle = settings.WindowTitle,
            WindowWidth = settings.WindowWidth,
            WindowHeight = settings.WindowHeight,
            Fullscreen = settings.Fullscreen,
            VSync = settings.VSync
        };

        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        var configPath = Path.Combine(buildOutput, "RuntimeConfig.json");
        File.WriteAllText(configPath, json);

        Logger.Information("Runtime config generated: {ConfigPath}", configPath);
    }

    private async Task<(bool Success, string ExePath, List<string> Errors)> BuildRuntimeAsync(
        BuildSettings settings, string buildOutput)
    {
        var errors = new List<string>();

        try
        {
            var runtimeProjectPath = Path.GetFullPath(Path.Combine(_projectPath, "..", "Runtime", "Runtime.csproj"));

            Logger.Information("Building Runtime project: {ProjectPath}", runtimeProjectPath);

            var arguments = $"publish \"{runtimeProjectPath}\" " +
                          $"-c {settings.Configuration} " +
                          $"-r {settings.GetRuntimeIdentifier()} " +
                          $"--self-contained true " +
                          $"-p:PublishSingleFile=true " +
                          $"-p:IncludeNativeLibrariesForSelfExtract=true " +
                          $"-p:EnableCompressionInSingleFile=true " +
                          $"-o \"{buildOutput}\"";

            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = arguments,
                WorkingDirectory = _projectPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Logger.Debug("Running: dotnet {Arguments}", arguments);

            using var process = new Process { StartInfo = psi };
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputBuilder.AppendLine(e.Data);
                    Logger.Debug("Build: {Output}", e.Data);
                }
            };

            process.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    errorBuilder.AppendLine(e.Data);
                    Logger.Warning("Build error: {Error}", e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                errors.Add($"Runtime build failed with exit code {process.ExitCode}");
                errors.Add(errorBuilder.ToString());
                return (false, "", errors);
            }

            // Find the generated executable
            var exeName = $"Runtime{settings.GetExecutableExtension()}";
            var exePath = Path.Combine(buildOutput, exeName);

            // Rename to game name
            var targetExeName = $"{settings.GameName}{settings.GetExecutableExtension()}";
            var targetExePath = Path.Combine(buildOutput, targetExeName);

            if (File.Exists(exePath))
            {
                if (File.Exists(targetExePath))
                {
                    File.Delete(targetExePath);
                }
                File.Move(exePath, targetExePath);
                Logger.Information("Runtime executable created: {ExePath}", targetExePath);
                return (true, targetExePath, errors);
            }
            else
            {
                errors.Add($"Runtime executable not found at: {exePath}");
                return (false, "", errors);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Exception during runtime build");
            errors.Add($"Runtime build exception: {ex.Message}");
            return (false, "", errors);
        }
    }

    private (int FileCount, long TotalSizeKB) CopyAssets(BuildSettings settings, string buildOutput)
    {
        var assetsSource = Path.Combine(_projectPath, "assets");
        var assetsTarget = Path.Combine(buildOutput, "GameData");

        if (!Directory.Exists(assetsSource))
        {
            Logger.Warning("Assets directory not found: {AssetsSource}", assetsSource);
            return (0, 0);
        }

        Directory.CreateDirectory(assetsTarget);

        int fileCount = 0;
        long totalSize = 0;

        // Copy all assets
        foreach (var file in Directory.GetFiles(assetsSource, "*.*", SearchOption.AllDirectories))
        {
            // Skip script files if they're pre-compiled
            if (settings.PrecompileScripts && file.EndsWith(".cs"))
            {
                continue;
            }

            var relativePath = Path.GetRelativePath(assetsSource, file);
            var targetPath = Path.Combine(assetsTarget, relativePath);

            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            File.Copy(file, targetPath, true);

            fileCount++;
            totalSize += new FileInfo(file).Length;
        }

        Logger.Information("Copied {FileCount} assets ({TotalSize} KB)", fileCount, totalSize / 1024);

        return (fileCount, totalSize / 1024);
    }

    private void GenerateBuildInfo(BuildSettings settings, string buildOutput, BuildReport report)
    {
        var buildInfo = $"""
            Build Information
            ==================
            Game: {settings.GameName}
            Build Time: {report.BuildTime:yyyy-MM-dd HH:mm:ss}
            Duration: {report.BuildDuration.TotalSeconds:F2}s
            Configuration: {settings.Configuration}
            Platform: {settings.Platform} ({settings.GetRuntimeIdentifier()})

            Settings:
            - Startup Scene: {settings.StartupScene}
            - Pre-compiled Scripts: {settings.PrecompileScripts}
            - Window: {settings.WindowWidth}x{settings.WindowHeight}
            - Fullscreen: {settings.Fullscreen}
            - VSync: {settings.VSync}

            Output:
            {string.Join("\n", report.Files.Select(f => $"- {f}"))}

            Total Assets: {report.AssetCount} files
            """;

        var buildInfoPath = Path.Combine(buildOutput, "BuildInfo.txt");
        File.WriteAllText(buildInfoPath, buildInfo);

        Logger.Information("Build info saved: {BuildInfoPath}", buildInfoPath);
    }

    private string? ResolveScenePath(string scenePath)
    {
        if (Path.IsPathRooted(scenePath) && File.Exists(scenePath))
            return scenePath;

        var paths = new[]
        {
            Path.Combine(_projectPath, scenePath),
            Path.Combine(_projectPath, "assets", scenePath),
            Path.Combine(_projectPath, "assets", "scenes", scenePath)
        };

        return paths.FirstOrDefault(File.Exists);
    }

    private long GetFileSize(string filePath)
    {
        return File.Exists(filePath) ? new FileInfo(filePath).Length / 1024 : 0;
    }

    private void LogProgress(string message)
    {
        _buildLog.AppendLine(message);
        OnBuildProgress?.Invoke(message);
    }
}

/// <summary>
/// Report generated after a build completes
/// </summary>
public class BuildReport
{
    public bool Success { get; set; }
    public DateTime BuildTime { get; set; }
    public TimeSpan BuildDuration { get; set; }
    public BuildSettings Settings { get; set; } = new();
    public string OutputDirectory { get; set; } = "";
    public List<string> Files { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public int AssetCount { get; set; }
    public bool CompiledScripts { get; set; }
    public string BuildLog { get; set; } = "";

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Build {(Success ? "SUCCESS" : "FAILED")}");
        sb.AppendLine($"Duration: {BuildDuration.TotalSeconds:F2}s");
        sb.AppendLine($"Output: {OutputDirectory}");

        if (Errors.Any())
        {
            sb.AppendLine($"\nErrors ({Errors.Count}):");
            foreach (var error in Errors)
            {
                sb.AppendLine($"  - {error}");
            }
        }

        return sb.ToString();
    }
}
