using Scripting;

namespace Engine.Scene.Systems;

public sealed class PhysicsContactQueue : IPhysicsContacts
{
    private readonly List<PhysicsContact> _pending = [];

    public void Enqueue(PhysicsContact contact) => _pending.Add(contact);

    public ReadOnlySpan<PhysicsContact> DrainContacts()
    {
        if (_pending.Count == 0)
            return ReadOnlySpan<PhysicsContact>.Empty;

        var snapshot = _pending.ToArray();
        _pending.Clear();
        return snapshot;
    }
}
