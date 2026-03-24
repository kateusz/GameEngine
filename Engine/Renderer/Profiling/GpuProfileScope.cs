namespace Engine.Renderer.Profiling;

public ref struct GpuProfileScope
{
    private readonly GpuTimerQueryPool? _pool;
    private readonly int _scopeId;
    private readonly uint _queryId;

    internal GpuProfileScope(GpuTimerQueryPool pool, int scopeId, uint queryId)
    {
        _pool = pool;
        _scopeId = scopeId;
        _queryId = queryId;
    }

    public void Dispose()
    {
        _pool?.EndQuery(_scopeId, _queryId);
    }
}
