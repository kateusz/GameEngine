using System.Diagnostics;

namespace Engine.Renderer.Profiling;

/// <summary>
/// Zero-allocation CPU timing + allocation tracking scope.
/// Use with: using var scope = profiler.BeginScope(scopeId);
/// </summary>
public ref struct ProfileScope
{
    private readonly IPerformanceProfiler? _profiler;
    private readonly int _scopeId;
    private readonly long _startTimestamp;
    private readonly long _startAllocations;

    internal ProfileScope(IPerformanceProfiler profiler, int scopeId)
    {
        _profiler = profiler;
        _scopeId = scopeId;
        _startAllocations = GC.GetAllocatedBytesForCurrentThread();
        _startTimestamp = Stopwatch.GetTimestamp();
    }

    public void Dispose()
    {
        if (_profiler is null) return;
        var elapsed = Stopwatch.GetElapsedTime(_startTimestamp);
        var allocDelta = GC.GetAllocatedBytesForCurrentThread() - _startAllocations;
        _profiler.RecordScopeResult(_scopeId, elapsed.TotalMilliseconds, allocDelta);
    }
}
