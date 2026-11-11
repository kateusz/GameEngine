using System.Diagnostics;

namespace Benchmark;

public sealed class BenchmarkProcessSampler
{
    private Process? _process;
    private DateTime _lastCpuCheck = DateTime.UtcNow;
    private TimeSpan _lastCpuTime;
    private readonly List<float> _cpu = new();
    private readonly List<long> _memMb = new();

    public IReadOnlyList<float> CpuSamples => _cpu;
    public IReadOnlyList<long> MemSamples => _memMb;

    public void Start() {
        _process = Process.GetCurrentProcess();
        _lastCpuTime = _process.TotalProcessorTime;
        _lastCpuCheck = DateTime.UtcNow;
    }

    public void Clear()
    {
        _cpu.Clear();
        _memMb.Clear();
    }

    public void SampleDuringMeasure()
    {
        if (_process == null)
            return;

        try
        {
            _process.Refresh();
            var now = DateTime.UtcNow;
            var cpuNow = _process.TotalProcessorTime;
            var elapsedMs = (now - _lastCpuCheck).TotalMilliseconds;
            if (elapsedMs > 200)
            {
                var cpuDelta = (cpuNow - _lastCpuTime).TotalMilliseconds;
                var usage = (float)(cpuDelta / (Environment.ProcessorCount * elapsedMs) * 100.0);
                _cpu.Add(System.Math.Clamp(usage, 0f, 100f * Environment.ProcessorCount));
                _lastCpuCheck = now;
                _lastCpuTime = cpuNow;
            }

            _memMb.Add(_process.WorkingSet64 / (1024 * 1024));
        }
        catch
        {
            // ponytail: sampling is best-effort
        }
    }
}
