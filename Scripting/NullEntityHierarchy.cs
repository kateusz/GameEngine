using System.Numerics;
using ECS;

namespace Scripting;

/// <summary>No-op hierarchy when no scene is active.</summary>
public sealed class NullEntityHierarchy : IEntityHierarchy
{
    public static readonly NullEntityHierarchy Instance = new();

    public Entity? GetParent(Entity entity) => null;
    public IReadOnlyList<Entity> GetChildren(Entity entity) => Array.Empty<Entity>();
    public bool SetParent(Entity child, Entity? parent) => false;
    public Vector3 GetWorldPosition(Entity entity) => Vector3.Zero;
}
