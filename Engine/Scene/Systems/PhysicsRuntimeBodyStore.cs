using Engine.Physics;

namespace Engine.Scene.Systems;

public sealed class PhysicsRuntimeBodyStore
{
    private readonly Dictionary<int, IPhysicsBody2D> _bodiesByEntityId = [];

    public bool TryGet(int entityId, out IPhysicsBody2D body) =>
        _bodiesByEntityId.TryGetValue(entityId, out body!);

    public void Set(int entityId, IPhysicsBody2D body) => _bodiesByEntityId[entityId] = body;

    public void Remove(int entityId) => _bodiesByEntityId.Remove(entityId);

    public IReadOnlyDictionary<int, IPhysicsBody2D> Snapshot() => _bodiesByEntityId;

    public void Clear() => _bodiesByEntityId.Clear();
}
