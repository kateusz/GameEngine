namespace Benchmark;

public record BenchmarkResult
{
    public string TestName { get; init; } = string.Empty;
    public int TotalFrames { get; init; }
    public float AverageFrameTime { get; init; }
    public float AverageFPS { get; init; }
    public float MinFPS { get; init; }
    public float MaxFPS { get; init; }
    public float Percentile99 { get; init; }
    public float TestDuration { get; init; }
    public float AverageCpuUsage { get; init; }
    public float MaxCpuUsage { get; init; }
    public float MinCpuUsage { get; init; }
    public long AverageMemoryUsageMB { get; init; }
    public long MaxMemoryUsageMB { get; init; }
    public long MinMemoryUsageMB { get; init; }

    public Dictionary<string, string> CustomMetrics { get; } = new();
}