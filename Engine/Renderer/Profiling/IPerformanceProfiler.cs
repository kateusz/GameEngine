namespace Engine.Renderer.Profiling;

public interface IPerformanceProfiler
{
    int RegisterScope(string name);
    int RegisterCounter(string name);
    int RegisterGauge(string name);

    ProfileScope BeginScope(int scopeId);
    ProfileScope BeginScope(string name);

    void IncrementCounter(int counterId, uint amount = 1);
    void IncrementCounter(string name, uint amount = 1);
    void SetGauge(int gaugeId, double value);
    void SetGauge(string name, double value);

    void BeginFrame();
    void EndFrame();

    IReadOnlyProfileData GetData();

    bool Enabled { get; set; }

    /// <summary>
    /// Called internally by ProfileScope.Dispose to record scope results.
    /// </summary>
    void RecordScopeResult(int scopeId, double elapsedMs, long allocatedBytes);
}
