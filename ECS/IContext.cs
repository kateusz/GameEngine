namespace ECS;

/// <summary>
/// Interface for managing the global entity registry.
/// Provides thread-safe access to entities and component queries.
/// </summary>
public interface IContext
{
    /// <summary>
    /// Gets a read-only view of all entities.
    /// </summary>
    IEnumerable<Entity> Entities { get; }

    /// <summary>
    /// Registers a new entity in the context.
    /// </summary>
    /// <param name="entity">The entity to register.</param>
    void Register(Entity entity);

    /// <summary>
    /// Removes an entity from the context by its ID.
    /// </summary>
    /// <param name="entityId">The ID of the entity to remove.</param>
    /// <returns>True if the entity was found and removed; false otherwise.</returns>
    bool Remove(int entityId);

    /// <summary>
    /// Clears all entities from the context.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets all entities that have all specified component types.
    /// </summary>
    /// <param name="types">The component types to filter by.</param>
    /// <returns>A list of entities matching the component filter.</returns>
    List<Entity> GetGroup(params Type[] types);

    /// <summary>
    /// Returns an enumerable view of all entities that have the specified component type.
    /// Uses value tuples and iterator pattern to dramatically reduce heap allocations.
    /// </summary>
    /// <typeparam name="TComponent">The type of component to query for.</typeparam>
    /// <returns>An enumerable of (Entity, TComponent) tuples representing each entity and its component.</returns>
    /// <remarks>
    /// Note: If you need to materialize the results into a collection, call .ToList() or .ToArray(),
    /// but be aware this will allocate. For best performance, consume results directly via foreach.
    /// </remarks>
    IEnumerable<(Entity Entity, TComponent Component)> View<TComponent>() where TComponent : IComponent;
}
