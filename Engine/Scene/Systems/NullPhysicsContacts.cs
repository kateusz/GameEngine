using Scripting;

namespace Engine.Scene.Systems;

internal sealed class NullPhysicsContacts : IPhysicsContacts
{
    public static readonly NullPhysicsContacts Instance = new();

    public ReadOnlySpan<PhysicsContact> DrainContacts() => ReadOnlySpan<PhysicsContact>.Empty;
}
