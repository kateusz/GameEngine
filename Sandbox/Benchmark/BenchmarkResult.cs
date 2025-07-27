namespace Sandbox.Benchmark;

public class BenchmarkResult
{
    public string TestName { get; set; } = string.Empty;
    public int TotalFrames { get; set; }
    public float AverageFrameTime { get; set; }
    public float AverageFPS { get; set; }
    public float MinFPS { get; set; }
    public float MaxFPS { get; set; }
    public float Percentile99 { get; set; }
    public float TestDuration { get; set; }
    public Dictionary<string, string> CustomMetrics { get; set; } = new();
}