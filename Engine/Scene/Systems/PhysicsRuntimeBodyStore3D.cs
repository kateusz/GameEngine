using Engine.Physics;

namespace Engine.Scene.Systems;

public sealed class PhysicsRuntimeBodyStore3D
{
    private readonly Dictionary<int, IPhysicsBody3D> _bodiesByEntityId = [];

    public bool TryGet(int entityId, out IPhysicsBody3D body) =>
        _bodiesByEntityId.TryGetValue(entityId, out body!);

    public void Set(int entityId, IPhysicsBody3D body) => _bodiesByEntityId[entityId] = body;

    public void Remove(int entityId) => _bodiesByEntityId.Remove(entityId);

    public IReadOnlyDictionary<int, IPhysicsBody3D> Snapshot() => _bodiesByEntityId;

    public void Clear() => _bodiesByEntityId.Clear();
}
