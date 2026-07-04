using ECS;

namespace Scripting;

public readonly record struct PhysicsContact(Entity Self, Entity Other, bool IsTrigger, bool IsBegin);

public interface IPhysicsContacts
{
    ReadOnlySpan<PhysicsContact> DrainContacts();
}
