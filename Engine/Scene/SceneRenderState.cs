using ECS;

namespace Engine.Scene;

/// <summary>Owns persistent rendering state for one scene identity on a render host.</summary>
internal sealed class SceneRenderState : IDisposable
{
    private Context? _identity;
    internal StaticMeshCache StaticMeshes { get; } = new();
    internal PointShadowCache PointShadows { get; } = new();

    internal bool Activate(Context identity)
    {
        if (ReferenceEquals(_identity, identity)) return false;
        StaticMeshes.Dispose();
        PointShadows.Clear();
        _identity = identity;
        return true;
    }

    public void Dispose()
    {
        StaticMeshes.Dispose(); PointShadows.Clear(); _identity = null;
    }
}
