using System.Diagnostics;

namespace Engine.Renderer.Profiling;

public class PerformanceProfiler : IPerformanceProfiler, IReadOnlyProfileData
{
    private const int DefaultRingBufferCapacity = 300;
    private const int MaxMetrics = 64;

    // Metric registration
    private readonly List<string> _scopeNames = [];
    private readonly List<string> _counterNames = [];
    private readonly List<string> _gaugeNames = [];
    private readonly Dictionary<string, int> _scopeNameToId = [];
    private readonly Dictionary<string, int> _counterNameToId = [];
    private readonly Dictionary<string, int> _gaugeNameToId = [];

    // Current frame accumulators (pre-allocated)
    private readonly double[] _currentScopeTimings = new double[MaxMetrics];
    private readonly long[] _currentAllocations = new long[MaxMetrics];
    private readonly uint[] _currentCounters = new uint[MaxMetrics];
    private readonly double[] _currentGauges = new double[MaxMetrics];

    // Pre-allocated backing arrays for snapshots
    private readonly double[][] _scopeTimingStorage;
    private readonly double[][] _gpuTimingStorage;
    private readonly long[][] _allocationStorage;
    private readonly uint[][] _counterStorage;
    private readonly double[][] _gaugeStorage;

    // Ring buffer of snapshots
    private readonly RingBuffer<FrameSnapshot> _history;
    private long _frameNumber;
    private long _frameStartTimestamp;

    public bool Enabled { get; set; }

    public PerformanceProfiler(int ringBufferCapacity = DefaultRingBufferCapacity)
    {
        _history = new RingBuffer<FrameSnapshot>(ringBufferCapacity);

        _scopeTimingStorage = new double[ringBufferCapacity][];
        _gpuTimingStorage = new double[ringBufferCapacity][];
        _allocationStorage = new long[ringBufferCapacity][];
        _counterStorage = new uint[ringBufferCapacity][];
        _gaugeStorage = new double[ringBufferCapacity][];

        for (var i = 0; i < ringBufferCapacity; i++)
        {
            _scopeTimingStorage[i] = new double[MaxMetrics];
            _gpuTimingStorage[i] = new double[MaxMetrics];
            _allocationStorage[i] = new long[MaxMetrics];
            _counterStorage[i] = new uint[MaxMetrics];
            _gaugeStorage[i] = new double[MaxMetrics];
        }
    }

    public int RegisterScope(string name)
    {
        if (_scopeNameToId.TryGetValue(name, out var existing))
            return existing;
        var id = _scopeNames.Count;
        _scopeNames.Add(name);
        _scopeNameToId[name] = id;
        return id;
    }

    public int RegisterCounter(string name)
    {
        if (_counterNameToId.TryGetValue(name, out var existing))
            return existing;
        var id = _counterNames.Count;
        _counterNames.Add(name);
        _counterNameToId[name] = id;
        return id;
    }

    public int RegisterGauge(string name)
    {
        if (_gaugeNameToId.TryGetValue(name, out var existing))
            return existing;
        var id = _gaugeNames.Count;
        _gaugeNames.Add(name);
        _gaugeNameToId[name] = id;
        return id;
    }

    public ProfileScope BeginScope(int scopeId)
    {
        if (!Enabled) return default;
        return new ProfileScope(this, scopeId);
    }

    public ProfileScope BeginScope(string name)
    {
        if (!Enabled) return default;
        var id = RegisterScope(name);
        return new ProfileScope(this, id);
    }

    public void RecordScopeResult(int scopeId, double elapsedMs, long allocatedBytes)
    {
        _currentScopeTimings[scopeId] += elapsedMs;
        _currentAllocations[scopeId] += allocatedBytes;
    }

    public void IncrementCounter(int counterId, uint amount = 1)
    {
        if (!Enabled) return;
        _currentCounters[counterId] += amount;
    }

    public void IncrementCounter(string name, uint amount = 1)
    {
        if (!Enabled) return;
        var id = RegisterCounter(name);
        _currentCounters[id] += amount;
    }

    public void SetGauge(int gaugeId, double value)
    {
        if (!Enabled) return;
        _currentGauges[gaugeId] = value;
    }

    public void SetGauge(string name, double value)
    {
        if (!Enabled) return;
        var id = RegisterGauge(name);
        _currentGauges[id] = value;
    }

    public void BeginFrame()
    {
        if (!Enabled) return;
        _frameStartTimestamp = Stopwatch.GetTimestamp();
        Array.Clear(_currentScopeTimings);
        Array.Clear(_currentAllocations);
        Array.Clear(_currentCounters);
        Array.Clear(_currentGauges);
    }

    public void EndFrame()
    {
        if (!Enabled) return;
        _frameNumber++;

        var totalMs = Stopwatch.GetElapsedTime(_frameStartTimestamp).TotalMilliseconds;
        var slot = (int)(_frameNumber % _history.Capacity);

        Array.Copy(_currentScopeTimings, _scopeTimingStorage[slot], MaxMetrics);
        Array.Copy(_currentAllocations, _allocationStorage[slot], MaxMetrics);
        Array.Copy(_currentCounters, _counterStorage[slot], MaxMetrics);
        Array.Copy(_currentGauges, _gaugeStorage[slot], MaxMetrics);
        // GPU timings written separately by GpuTimerQueryPool

        var snapshot = new FrameSnapshot
        {
            FrameNumber = _frameNumber,
            TotalFrameTimeMs = totalMs,
            ScopeTimingsMs = _scopeTimingStorage[slot].AsMemory(0, _scopeNames.Count),
            GpuTimingsMs = _gpuTimingStorage[slot].AsMemory(0, _scopeNames.Count),
            Allocations = _allocationStorage[slot].AsMemory(0, _scopeNames.Count),
            Counters = _counterStorage[slot].AsMemory(0, _counterNames.Count),
            Gauges = _gaugeStorage[slot].AsMemory(0, _gaugeNames.Count),
        };

        _history.Push(snapshot);
    }

    public IReadOnlyProfileData GetData() => this;

    // IReadOnlyProfileData implementation
    public FrameSnapshot Latest => _history.Count > 0 ? _history[0] : default;

    public ReadOnlySpan<FrameSnapshot> GetHistory(int frameCount)
    {
        var count = System.Math.Min(frameCount, _history.Count);
        return _history.AsSpan()[^count..];
    }

    public IReadOnlyList<string> RegisteredScopes => _scopeNames;
    public IReadOnlyList<string> RegisteredCounters => _counterNames;
    public IReadOnlyList<string> RegisteredGauges => _gaugeNames;

    public double GetScopeTimingMs(string name) =>
        _scopeNameToId.TryGetValue(name, out var id) && Latest.ScopeTimingsMs.Length > id
            ? Latest.ScopeTimingsMs.Span[id] : 0;

    public double GetGpuTimingMs(string name) =>
        _scopeNameToId.TryGetValue(name, out var id) && Latest.GpuTimingsMs.Length > id
            ? Latest.GpuTimingsMs.Span[id] : 0;

    public long GetAllocation(string name) =>
        _scopeNameToId.TryGetValue(name, out var id) && Latest.Allocations.Length > id
            ? Latest.Allocations.Span[id] : 0;

    public uint GetCounterValue(string name) =>
        _counterNameToId.TryGetValue(name, out var id) && Latest.Counters.Length > id
            ? Latest.Counters.Span[id] : 0;

    public double GetGaugeValue(string name) =>
        _gaugeNameToId.TryGetValue(name, out var id) && Latest.Gauges.Length > id
            ? Latest.Gauges.Span[id] : 0;

    public int GetScopeId(string name) =>
        _scopeNameToId.TryGetValue(name, out var id) ? id : -1;

    public int GetCounterId(string name) =>
        _counterNameToId.TryGetValue(name, out var id) ? id : -1;

    public int GetGaugeId(string name) =>
        _gaugeNameToId.TryGetValue(name, out var id) ? id : -1;

    /// <summary>
    /// Called by GpuTimerQueryPool to write GPU timing results for a previous frame.
    /// </summary>
    internal void RecordGpuTiming(int scopeId, double elapsedMs, long frameNumber)
    {
        var slot = (int)(frameNumber % _history.Capacity);
        _gpuTimingStorage[slot][scopeId] = elapsedMs;
    }
}
