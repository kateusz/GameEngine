namespace Benchmark;

public readonly record struct BenchmarkSettings(int Count, float DurationSeconds);

public readonly record struct BenchmarkTestButton(string Id, string Label);

public interface IBenchmarkRunner
{
    string Title { get; }
    BenchmarkSettings Settings { get; }
    IReadOnlyList<BenchmarkTestButton> Tests { get; }
    void DrawControls();
    bool TryCreate(string testId, BenchmarkSettings settings, out BenchmarkRun? run, out string? error);
}
