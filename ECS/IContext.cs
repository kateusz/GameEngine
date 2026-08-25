namespace ECS;

/// <summary>
/// Interface for a scene-owned entity registry.
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
    
    Entity GetByName(string name);
    
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
    /// All registered entities in insertion order.
    /// </summary>
    IEnumerable<Entity> Entities { get; }

    /// <summary>
    /// Returns whether an entity with the given ID is registered.
    /// </summary>
    bool Contains(int entityId);

    /// <summary>
    /// Entities with <typeparamref name="TComponent"/> (indexed; O(matches) not O(all entities)).
    /// </summary>
    ComponentView<TComponent> View<TComponent>() where TComponent : IComponent;

    /// <summary>
    /// Entities with both component types. Iterates the smaller index.
    /// </summary>
    DualComponentView<T1, T2> View<T1, T2>()
        where T1 : IComponent
        where T2 : IComponent;
}
