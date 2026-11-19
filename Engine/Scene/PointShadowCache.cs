using System.Numerics;
using Engine.Renderer;

namespace Engine.Scene;

internal sealed class PointShadowCache
{
    internal readonly record struct CasterPose(Matrix4x4 World, Aabb Bounds, bool HasBounds);
    internal readonly record struct LampPose(Vector3 Position, float Range);

    private readonly Dictionary<int, CasterPose> Casters = new();
    private readonly Dictionary<int, LampPose> Lamps = new();
    private readonly HashSet<int> Dirty = new();
    private readonly HashSet<int> Seen = new();
    private bool _stale = true;

    public void Clear()
    {
        Casters.Clear();
        Lamps.Clear();
        Dirty.Clear();
        Seen.Clear();
        _stale = true;
    }

    public void MarkStale() => _stale = true;

    public bool NeedsRedraw(
        int lampId, Vector3 position, float range,
        IReadOnlyList<(int Id, CasterPose Pose)> current)
    {
        if (_stale || Dirty.Contains(lampId) || !Lamps.TryGetValue(lampId, out var previousLamp))
            return true;
        if (previousLamp.Position != position || previousLamp.Range != range)
            return true;

        Seen.Clear();
        foreach (var (id, pose) in current)
        {
            Seen.Add(id);
            if (!Casters.TryGetValue(id, out var previous))
            {
                if (!pose.HasBounds || LightingMath.PointShadowSphereHits(position, range, pose.World, pose.Bounds))
                    return true;
                continue;
            }

            var moved = previous.World != pose.World
                || previous.HasBounds != pose.HasBounds
                || previous.Bounds != pose.Bounds;
            if (!moved)
                continue;
            if (!pose.HasBounds || !previous.HasBounds)
                return true;
            if (LightingMath.PointShadowSphereHits(previousLamp.Position, previousLamp.Range, previous.World, previous.Bounds)
                || LightingMath.PointShadowSphereHits(position, range, pose.World, pose.Bounds))
                return true;
        }

        foreach (var (id, previous) in Casters)
        {
            if (Seen.Contains(id))
                continue;
            if (!previous.HasBounds || LightingMath.PointShadowSphereHits(previousLamp.Position, previousLamp.Range, previous.World, previous.Bounds))
                return true;
        }

        return false;
    }

    public void RememberDirty(int lampId) => Dirty.Add(lampId);

    public void RememberClean(int lampId) => Dirty.Remove(lampId);

    public void Replace(
        IReadOnlyList<(int Id, CasterPose Pose)> casters,
        IReadOnlyList<(int Id, LampPose Pose)> lamps)
    {
        Casters.Clear();
        foreach (var (id, pose) in casters)
            Casters[id] = pose;

        Lamps.Clear();
        foreach (var (id, pose) in lamps)
            Lamps[id] = pose;

        _stale = false;
    }
}
