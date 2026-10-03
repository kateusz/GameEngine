namespace Benchmark;

public sealed class ActiveRun
{
    public BenchmarkRun? Run { get; private set; }

    public void Adopt(BenchmarkRun run) => Run = run;

    public bool Finish(bool commit, int sampleCount, Action<BenchmarkRun> store)
    {
        if (Run == null)
            return false;

        var stored = false;
        if (commit && sampleCount > 0)
        {
            store(Run);
            stored = true;
        }

        var next = commit ? Run.NextPhase() : null;
        Run.Dispose();
        Run = next;
        return stored;
    }
}
