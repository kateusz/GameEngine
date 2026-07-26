using System.Diagnostics;

namespace Editor.Publisher;

public partial class GamePublisher
{
    private static async Task<PublishResult> BuildRuntimeAsync(
        PublishSettings settings,
        string outputPath,
        List<string> buildOutput,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        var runtimeProjectPath = RuntimeProjectLocator.FindRuntimeProjectPath();
        if (runtimeProjectPath is null)
        {
            const string error = "Could not find Runtime.csproj. Ensure the Runtime project exists in the solution.";
            Logger.Error(error);
            return PublishResult.Failed(error);
        }

        var arguments = BuildDotnetPublishArguments(settings, runtimeProjectPath, outputPath);

        Logger.Information("Running: dotnet {Arguments}", arguments);
        progress?.Report("Compiling runtime executable...");

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        // Avoid MSBuild node-reuse holding the publish process open after output is written.
        psi.Environment["MSBUILDDISABLENODEREUSE"] = "1";

        try
        {
            using var process = new Process { StartInfo = psi };

            if (!process.Start())
                return PublishResult.Failed("Failed to start dotnet process.");

            // Read streams concurrently — BeginOutputReadLine + WaitForExitAsync can hang
            // when MSBuild node-reuse leaves pipes open after the build finishes.
            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            AppendBuildOutput(buildOutput, stdout, progress, isError: false);
            AppendBuildOutput(buildOutput, stderr, progress, isError: true);

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

    private static void AppendBuildOutput(
        List<string> buildOutput,
        string text,
        IProgress<string>? progress,
        bool isError)
    {
        if (string.IsNullOrEmpty(text))
            return;

        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var message = isError ? $"ERROR: {line}" : line;
            buildOutput.Add(message);
            if (isError)
                Logger.Error("Build error: {ErrorData}", line);
            else
                Logger.Debug("{BuildOutput}", line);
            progress?.Report(message);
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
            args += " /p:PublishSingleFile=true";

        return args;
    }
}
