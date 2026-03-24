using Engine.Renderer;

namespace Engine.Renderer.Profiling;

public class GpuTimerQueryPool(IRendererAPI rendererApi, PerformanceProfiler profiler) : IDisposable
{
    private const int PoolSize = 32;
    private const int BufferCount = 3; // triple buffer for safe async readback

    private readonly uint[][] _queryIds = new uint[BufferCount][];
    private readonly int[][] _scopeIds = new int[BufferCount][];
    private readonly int[] _queryCounts = new int[BufferCount];
    private readonly long[] _frameNumbers = new long[BufferCount];
    private int _writeBuffer;
    private bool _initialized;

    public void Initialize()
    {
        for (var b = 0; b < BufferCount; b++)
        {
            _queryIds[b] = new uint[PoolSize];
            _scopeIds[b] = new int[PoolSize];
            for (var i = 0; i < PoolSize; i++)
                _queryIds[b][i] = rendererApi.CreateTimerQuery();
        }
        _initialized = true;
    }

    public GpuProfileScope BeginQuery(int scopeId)
    {
        if (!_initialized) return default;
        var buf = _writeBuffer;
        var idx = _queryCounts[buf];
        if (idx >= PoolSize) return default; // pool exhausted this frame

        _scopeIds[buf][idx] = scopeId;
        _queryCounts[buf]++;
        rendererApi.BeginTimerQuery(_queryIds[buf][idx]);
        return new GpuProfileScope(this, scopeId, _queryIds[buf][idx]);
    }

    internal void EndQuery(int scopeId, uint queryId)
    {
        rendererApi.EndTimerQuery();
    }

    public void BeginFrame(long frameNumber)
    {
        // Read results from oldest buffer (2 frames ago)
        var readBuffer = (_writeBuffer + 1) % BufferCount;
        ReadResults(readBuffer);

        // Reset current write buffer
        _queryCounts[_writeBuffer] = 0;
        _frameNumbers[_writeBuffer] = frameNumber;
    }

    public void EndFrame()
    {
        _writeBuffer = (_writeBuffer + 1) % BufferCount;
    }

    private void ReadResults(int bufferIndex)
    {
        var count = _queryCounts[bufferIndex];
        if (count == 0) return;

        var frameNum = _frameNumbers[bufferIndex];
        for (var i = 0; i < count; i++)
        {
            var queryId = _queryIds[bufferIndex][i];
            if (!rendererApi.IsTimerQueryResultAvailable(queryId))
                continue;

            var nanoseconds = rendererApi.GetTimerQueryResult(queryId);
            var ms = nanoseconds / 1_000_000.0;
            profiler.RecordGpuTiming(_scopeIds[bufferIndex][i], ms, frameNum);
        }
    }

    public void Dispose()
    {
        if (!_initialized) return;
        for (var b = 0; b < BufferCount; b++)
            for (var i = 0; i < PoolSize; i++)
                rendererApi.DeleteTimerQuery(_queryIds[b][i]);
    }
}
