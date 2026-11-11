using Engine.Renderer;

namespace Benchmark;

public static class BenchmarkResultBuilder
{
    public static BenchmarkResult Build(
        BenchmarkRun run,
        IReadOnlyList<float> frameTimesMs,
        float measuredWallSeconds,
        int frameCount,
        float avgCpu,
        float maxCpu,
        float minCpu,
        long avgMemMb,
        long maxMemMb,
        long minMemMb,
        Graphics2DStatsAggregator? stats2D,
        PipelineStatsAggregator? stats3D)
    {
        if (frameTimesMs.Count == 0)
        {
            return new BenchmarkResult
            {
                TestName = run.ResultName,
                TotalFrames = 0,
                TestDuration = measuredWallSeconds
            };
        }

        var sorted = frameTimesMs.ToArray();
        Array.Sort(sorted);
        var avg = sorted.Average();
        var p95 = Percentile(sorted, 0.95);
        var p99 = Percentile(sorted, 0.99);
        var median = Percentile(sorted, 0.5);

        var result = new BenchmarkResult
        {
            TestName = run.ResultName,
            TotalFrames = frameCount,
            AverageFrameTime = (float)avg,
            MinFPS = 1000f / sorted[^1],
            MaxFPS = 1000f / sorted[0],
            AverageFPS = 1000f / (float)avg,
            Percentile99 = p99,
            TestDuration = measuredWallSeconds,
            AverageCpuUsage = avgCpu,
            MaxCpuUsage = maxCpu,
            MinCpuUsage = minCpu,
            AverageMemoryUsageMB = avgMemMb,
            MaxMemoryUsageMB = maxMemMb,
            MinMemoryUsageMB = minMemMb
        };

        result.CustomMetrics["Median Frame Time Ms"] = median.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
        result.CustomMetrics["P95 Frame Time Ms"] = p95.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);

        stats2D?.ApplyTo(result);
        stats3D?.ApplyTo(result);
        run.Contribute(result);
        return result;
    }

    public static float Percentile(float[] sortedAscending, double p)
    {
        if (sortedAscending.Length == 0)
            return 0f;
        var index = (int)System.Math.Clamp(System.Math.Ceiling(p * sortedAscending.Length) - 1, 0, sortedAscending.Length - 1);
        return sortedAscending[index];
    }
}
