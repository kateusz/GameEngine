namespace Engine.Renderer.Profiling;

public readonly record struct FrameSnapshot
{
    public long FrameNumber { get; init; }
    public double TotalFrameTimeMs { get; init; }
    public ReadOnlyMemory<double> ScopeTimingsMs { get; init; }
    public ReadOnlyMemory<double> GpuTimingsMs { get; init; }
    public ReadOnlyMemory<long> Allocations { get; init; }
    public ReadOnlyMemory<uint> Counters { get; init; }
    public ReadOnlyMemory<double> Gauges { get; init; }
}
