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
        string rendererMode, string? frameSamplesCsv)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var sorted = result.CustomMetrics.TryGetValue("Median Frame Time Ms", out _)
            ? (float)result.AverageFrameTime
            : result.AverageFrameTime;
        float.TryParse(result.CustomMetrics.GetValueOrDefault("Median Frame Time Ms"),
            NumberStyles.Float, CultureInfo.InvariantCulture, out var median);
        float.TryParse(result.CustomMetrics.GetValueOrDefault("P95 Frame Time Ms"),
            NumberStyles.Float, CultureInfo.InvariantCulture, out var p95);

        var payload = new Dictionary<string, object?>
        {
            ["schema"] = "gameengine-perf/1",
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
            ["measuredWallSeconds"] = result.TestDuration,
            ["frameCount"] = result.TotalFrames,
            ["avgFps"] = result.AverageFPS,
            ["avgFrameTimeMs"] = result.AverageFrameTime,
            ["medianFrameTimeMs"] = median > 0 ? median : result.AverageFrameTime,
            ["p95FrameTimeMs"] = p95 > 0 ? p95 : result.Percentile99,
            ["p99FrameTimeMs"] = result.Percentile99,
            ["rendererMode"] = rendererMode,
            ["customMetrics"] = result.CustomMetrics,
            ["frameSamplesCsv"] = frameSamplesCsv,
            ["success"] = success,
            ["error"] = error
        };

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
}
