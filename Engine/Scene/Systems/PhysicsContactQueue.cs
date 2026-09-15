using ECS;
using Engine.Physics;
using Scripting;

namespace Engine.Scene.Systems;

public sealed class PhysicsContactQueue : IPhysicsContacts, IPhysicsContactListener
{
    private readonly List<PhysicsContact> _pending = [];

    public void Enqueue(PhysicsContact contact) => _pending.Add(contact);

    public void OnContactBegin(IPhysicsBody2D bodyA, IPhysicsBody2D bodyB, bool isTrigger) =>
        EnqueuePair(bodyA.Entity, bodyB.Entity, isTrigger, isBegin: true);

    public void OnContactEnd(IPhysicsBody2D bodyA, IPhysicsBody2D bodyB, bool isTrigger) =>
        EnqueuePair(bodyA.Entity, bodyB.Entity, isTrigger, isBegin: false);

    public ReadOnlySpan<PhysicsContact> DrainContacts()
    {
        if (_pending.Count == 0)
            return ReadOnlySpan<PhysicsContact>.Empty;

        var snapshot = _pending.ToArray();
        _pending.Clear();
        return snapshot;
    }

    private void EnqueuePair(Entity? entityA, Entity? entityB, bool isTrigger, bool isBegin)
    {
        if (entityA == null || entityB == null)
            return;

        _pending.Add(new PhysicsContact(entityA, entityB, isTrigger, isBegin));
        _pending.Add(new PhysicsContact(entityB, entityA, isTrigger, isBegin));
    }
}
