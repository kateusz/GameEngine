using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace Benchmark.Cli;

public static class PerfMetricsWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static void WriteJson(string path, BenchmarkResult result, PerfRunOptions opts, bool success, string? error,
        string rendererMode, string? frameSamplesCsv,
        IReadOnlyList<float>? iterationAvgFps = null,
        IReadOnlyList<float>? iterationAvgFrameTimeMs = null)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        float.TryParse(result.CustomMetrics.GetValueOrDefault("Median Frame Time Ms"),
            NumberStyles.Float, CultureInfo.InvariantCulture, out var median);
        float.TryParse(result.CustomMetrics.GetValueOrDefault("P95 Frame Time Ms"),
            NumberStyles.Float, CultureInfo.InvariantCulture, out var p95);

        var payload = new Dictionary<string, object?>
        {
            ["schema"] = "gameengine-perf/1",
            ["gitCommitSha"] = TryResolveGitHead(),
            ["scene"] = $"{opts.Category}:{opts.TestId}",
            ["category"] = opts.Category,
            ["testId"] = opts.TestId,
            ["count"] = opts.Count,
            ["seed"] = opts.Seed,
            ["width"] = opts.Width,
            ["height"] = opts.Height,
            ["targetFrameRate"] = opts.TargetFrameRate,
            ["warmupSeconds"] = opts.WarmupSeconds,
            ["measureSeconds"] = opts.MeasureSeconds,
            ["measureIterations"] = opts.MeasureIterations,
            ["measuredWallSeconds"] = result.TestDuration,
            ["frameCount"] = result.TotalFrames,
            ["avgFps"] = result.AverageFPS,
            ["minFps"] = result.MinFPS,
            ["maxFps"] = result.MaxFPS,
            ["avgFrameTimeMs"] = result.AverageFrameTime,
            ["medianFrameTimeMs"] = median > 0 ? median : result.AverageFrameTime,
            ["p95FrameTimeMs"] = p95 > 0 ? p95 : result.Percentile99,
            ["p99FrameTimeMs"] = result.Percentile99,
            ["avgCpuUsagePct"] = result.AverageCpuUsage,
            ["maxCpuUsagePct"] = result.MaxCpuUsage,
            ["minCpuUsagePct"] = result.MinCpuUsage,
            ["avgMemoryUsageMb"] = result.AverageMemoryUsageMB,
            ["maxMemoryUsageMb"] = result.MaxMemoryUsageMB,
            ["minMemoryUsageMb"] = result.MinMemoryUsageMB,
            ["rendererMode"] = rendererMode,
            ["customMetrics"] = result.CustomMetrics,
            ["frameSamplesCsv"] = frameSamplesCsv,
            ["success"] = success,
            ["error"] = error
        };

        if (iterationAvgFps is { Count: > 0 })
            payload["iterationAvgFps"] = iterationAvgFps;
        if (iterationAvgFrameTimeMs is { Count: > 0 })
            payload["iterationAvgFrameTimeMs"] = iterationAvgFrameTimeMs;

        File.WriteAllText(path, JsonSerializer.Serialize(payload, JsonOptions));
    }

    public static void WriteFrameCsv(string path, IReadOnlyList<float> frameTimesMs)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        using var writer = new StreamWriter(path);
        writer.WriteLine("frame_index,frame_time_ms");
        for (var i = 0; i < frameTimesMs.Count; i++)
            writer.WriteLine(FormattableString.Invariant($"{i},{frameTimesMs[i]:R}"));
    }

    private static string TryResolveGitHead()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "rev-parse HEAD",
                WorkingDirectory = AppContext.BaseDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            if (process == null)
                return "";
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            try
            {
                Task.WhenAll(stdout, stderr, process.WaitForExitAsync())
                    .WaitAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();
            }
            catch (TimeoutException)
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
                return "";
            }

            return process.ExitCode == 0 ? stdout.GetAwaiter().GetResult().Trim() : "";
        }
        catch
        {
            return "";
        }
    }
}
