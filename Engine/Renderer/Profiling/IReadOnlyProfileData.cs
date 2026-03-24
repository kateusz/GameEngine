namespace Engine.Renderer.Profiling;

public interface IReadOnlyProfileData
{
    FrameSnapshot Latest { get; }
    ReadOnlySpan<FrameSnapshot> GetHistory(int frameCount);
    IReadOnlyList<string> RegisteredScopes { get; }
    IReadOnlyList<string> RegisteredCounters { get; }
    IReadOnlyList<string> RegisteredGauges { get; }
    double GetScopeTimingMs(string name);
    double GetGpuTimingMs(string name);
    long GetAllocation(string name);
    uint GetCounterValue(string name);
    double GetGaugeValue(string name);
    int GetScopeId(string name);
    int GetCounterId(string name);
    int GetGaugeId(string name);
}
