using Box2D.NetStandard.Dynamics.Bodies;

namespace Engine.Scene.Systems;

public sealed class PhysicsRuntimeBodyStore
{
    private readonly Dictionary<int, Body> _bodiesByEntityId = [];

    public bool TryGet(int entityId, out Body body) => _bodiesByEntityId.TryGetValue(entityId, out body!);

    public void Set(int entityId, Body body) => _bodiesByEntityId[entityId] = body;

    public void Remove(int entityId) => _bodiesByEntityId.Remove(entityId);

    public IReadOnlyDictionary<int, Body> Snapshot() => _bodiesByEntityId;

    public void Clear() => _bodiesByEntityId.Clear();
}
