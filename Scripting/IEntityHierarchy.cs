using System.Numerics;
using ECS;

namespace Scripting;

/// <summary>
/// Parent-child entity hierarchy. Implemented by the active scene.
/// </summary>
public interface IEntityHierarchy
{
    Entity? GetParent(Entity entity);

    IReadOnlyList<Entity> GetChildren(Entity entity);

    /// <summary>
    /// Reparent <paramref name="child"/> under <paramref name="parent"/> (null = scene root).
    /// Returns false if the operation would create a cycle or either entity is invalid.
    /// </summary>
    bool SetParent(Entity child, Entity? parent);

    /// <summary>World-space translation for an entity (identity/zero if no transform).</summary>
    Vector3 GetWorldPosition(Entity entity);
}
