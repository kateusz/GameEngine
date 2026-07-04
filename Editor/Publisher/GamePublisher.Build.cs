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

        try
        {
            using var process = new Process { StartInfo = psi };

            process.OutputDataReceived += (_, e) =>
            {
                if (string.IsNullOrEmpty(e.Data))
                    return;

                buildOutput.Add(e.Data);
                Logger.Debug("{BuildOutput}", e.Data);
                progress?.Report(e.Data);
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (string.IsNullOrEmpty(e.Data))
                    return;

                buildOutput.Add($"ERROR: {e.Data}");
                Logger.Error("Build error: {ErrorData}", e.Data);
                progress?.Report($"ERROR: {e.Data}");
            };

            if (!process.Start())
                return PublishResult.Failed("Failed to start dotnet process.");

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
            args += " /p:PublishSingleFile=true";

        return args;
    }
}
