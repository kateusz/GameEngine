namespace ECS;

/// <summary>
/// Interface for managing the global entity registry.
/// Provides thread-safe access to entities and component queries.
/// </summary>
public interface IContext
{
    /// <summary>
    /// Gets Entity by Id
    /// </summary>
    /// <param name="entityId">Id of Entity</param>
    /// <returns><see cref="Entity"/></returns>
    Entity GetById(int entityId);
    
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
    /// Returns an enumerable view of all entities that have the specified component type.
    /// </summary>
    /// <typeparam name="TComponent">The type of component to query for.</typeparam>
    /// <returns>An enumerable of (Entity, TComponent) tuples representing each entity and its component.</returns>
    /// <remarks>
    /// Note: For best performance, consume results directly via foreach.
    /// </remarks>
    IEnumerable<(Entity Entity, TComponent Component)> View<TComponent>() where TComponent : IComponent;
}
