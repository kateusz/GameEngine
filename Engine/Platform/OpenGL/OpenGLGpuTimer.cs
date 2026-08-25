using Engine.Platform.SilkNet;
using Silk.NET.OpenGL;

namespace Engine.Platform.OpenGL;

/// <summary>
/// OpenGL 3.3 TIME_ELAPSED query — result available one frame after End().
/// </summary>
internal sealed class OpenGLGpuTimer : IDisposable
{
    private uint _queryId;
    private bool _disposed;
    private bool _pending;

    public OpenGLGpuTimer()
    {
        _queryId = SilkNetContext.GL.GenQuery();
    }

    public void Begin()
    {
        SilkNetContext.GL.BeginQuery(QueryTarget.TimeElapsed, _queryId);
    }

    public void End()
    {
        SilkNetContext.GL.EndQuery(QueryTarget.TimeElapsed);
        _pending = true;
    }

    public bool TryGetElapsedMs(out double ms)
    {
        ms = 0;
        if (!_pending)
            return false;

        var available = 0;
        unsafe
        {
            SilkNetContext.GL.GetQueryObject(_queryId, GLEnum.QueryResultAvailable, &available);
            if (available == 0)
                return false;

            ulong elapsedNs = 0;
            SilkNetContext.GL.GetQueryObject(_queryId, GLEnum.QueryResult, &elapsedNs);
            ms = elapsedNs / 1_000_000.0;
        }

        _pending = false;
        return true;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (_queryId != 0)
        {
            SilkNetContext.GL.DeleteQuery(_queryId);
            _queryId = 0;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
