namespace Benchmark;

public abstract class BenchmarkRun : IDisposable
{
    public abstract string ResultName { get; }
    public abstract float DurationSeconds { get; }
    public virtual bool SamplesRenderer2D => false;
    public abstract void Tick(TimeSpan delta);
    public abstract void Contribute(BenchmarkResult result);
    public virtual BenchmarkRun? NextPhase() => null;
    public abstract void Dispose();
}
