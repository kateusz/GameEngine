using Engine.Renderer;

namespace Benchmark;

/// <summary>
/// Aggregates per-frame <see cref="Graphics2DStats"/> samples from a benchmark run.
/// </summary>
internal sealed class Graphics2DStatsAggregator
{
    private readonly List<Graphics2DStats> _samples = [];

    public void AddSample(Graphics2DStats stats) => _samples.Add(stats with { });

    public int SampleCount => _samples.Count;

    public void Clear() => _samples.Clear();

    public void ApplyTo(BenchmarkResult result)
    {
        if (_samples.Count == 0)
            return;

        result.CustomMetrics["Avg Quad Draw Calls"] = Avg(s => s.DrawCalls).ToString("F1");
        result.CustomMetrics["Avg Line Draw Calls"] = Avg(s => s.LineDrawCalls).ToString("F1");
        result.CustomMetrics["Avg Quads"] = Avg(s => s.QuadCount).ToString("F0");
        result.CustomMetrics["Avg Line Vertices"] = Avg(s => s.LineVertexCount).ToString("F0");
        result.CustomMetrics["Avg Batch Count"] = Avg(s => s.BatchCount).ToString("F2");
        result.CustomMetrics["Avg Texture Binds"] = Avg(s => s.TextureBinds).ToString("F1");
        result.CustomMetrics["Avg Program Switches"] = Avg(s => s.ProgramSwitches).ToString("F1");
        result.CustomMetrics["Avg Upload KB"] = (Avg(s => s.UploadBytes) / 1024.0).ToString("F1");
        result.CustomMetrics["Avg BatchFill Ms"] = Avg(s => s.BatchFillMs).ToString("F3");
        result.CustomMetrics["Avg Flush Ms"] = Avg(s => s.FlushMs).ToString("F3");
        result.CustomMetrics["Max Flush Ms"] = _samples.Max(s => s.FlushMs).ToString("F3");
        result.CustomMetrics["Avg GPU Quad Ms"] = Avg(s => s.GpuQuadPassMs).ToString("F3");
        result.CustomMetrics["Avg GPU Line Ms"] = Avg(s => s.GpuLinePassMs).ToString("F3");
        result.CustomMetrics["Max Upload KB"] = (_samples.Max(s => s.UploadBytes) / 1024.0).ToString("F1");
    }

    private double Avg(Func<Graphics2DStats, double> selector) =>
        _samples.Average(s => selector(s));
}
