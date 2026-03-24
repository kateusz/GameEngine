using ECS.Systems;

namespace Engine.Renderer.Profiling;

public class SystemProfilerAdapter(IPerformanceProfiler profiler) : ISystemProfiler
{
    public IDisposable BeginSystemScope(string systemName)
    {
        return new ScopeWrapper(profiler, profiler.RegisterScope(systemName));
    }

    private class ScopeWrapper(IPerformanceProfiler profiler, int scopeId) : IDisposable
    {
        private readonly long _startTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();
        private readonly long _startAlloc = GC.GetAllocatedBytesForCurrentThread();
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(_startTimestamp);
            var allocDelta = GC.GetAllocatedBytesForCurrentThread() - _startAlloc;
            profiler.RecordScopeResult(scopeId, elapsed.TotalMilliseconds, allocDelta);
        }
    }
}
