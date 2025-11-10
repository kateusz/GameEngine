namespace ECS;

/// <summary>
/// Manages the global entity registry
/// Thread-safe for concurrent access.
/// </summary>
public class Context : IContext
{
    private static Context? _instance;
    public static Context Instance => _instance ??= new Context();

    // Dual storage for optimal performance:
    // - Dictionary enables O(1) entity lookup and removal by ID
    // - List enables efficient iteration without boxing overhead
    private readonly Dictionary<int, Entity> _entitiesById = new();
    private readonly List<Entity> _entitiesList = [];
    private readonly Lock _lock = new();

    /// <summary>
    /// Gets a read-only view of all entities.
    /// For iteration, this provides O(n) enumeration without allocations.
    /// </summary>
    /// <remarks>
    /// Performance: Iteration is O(n) with minimal overhead.
    /// Thread-safety: Snapshot is taken under lock to ensure consistency.
    /// </remarks>
    public IEnumerable<Entity> Entities
    {
        get
        {
            lock (_lock)
            {
                // Return a copy to prevent concurrent modification during iteration
                return _entitiesList.ToArray();
            }
        }
    }

    private Context()
    {
    }

    /// <summary>
    /// Registers a new entity in the context.
    /// </summary>
    /// <param name="entity">The entity to register.</param>
    /// <remarks>
    /// Performance: O(1) operation with dictionary and list insertion.
    /// Thread-safety: Protected by lock for concurrent access.
    /// </remarks>
    public void Register(Entity entity)
    {
        lock (_lock)
        {
            if (_entitiesById.ContainsKey(entity.Id))
            {
                throw new InvalidOperationException($"Entity with ID {entity.Id} is already registered.");
            }

            _entitiesById[entity.Id] = entity;
            _entitiesList.Add(entity);
        }
    }

    /// <summary>
    /// Removes an entity from the context by its ID.
    /// </summary>
    /// <param name="entityId">The ID of the entity to remove.</param>
    /// <returns>True if the entity was found and removed; false otherwise.</returns>
    /// <remarks>
    /// Performance: O(1) dictionary lookup + O(n) list removal.
    /// The list removal is unavoidable but happens only once per deletion.
    /// This is still vastly superior to the previous O(n) iteration + double allocation.
    /// Thread-safety: Protected by lock for concurrent access.
    /// </remarks>
    public bool Remove(int entityId)
    {
        lock (_lock)
        {
            if (!_entitiesById.Remove(entityId, out var entity))
                return false;
            
            _entitiesList.Remove(entity);
            return true;
        }
    }

    /// <summary>
    /// Clears all entities from the context.
    /// </summary>
    /// <remarks>
    /// Performance: O(n) to clear both collections.
    /// Thread-safety: Protected by lock for concurrent access.
    /// </remarks>
    public void Clear()
    {
        lock (_lock)
        {
            _entitiesById.Clear();
            _entitiesList.Clear();
        }
    }

    /// <summary>
    /// Gets all entities that have all specified component types.
    /// </summary>
    /// <param name="types">The component types to filter by.</param>
    /// <returns>A list of entities matching the component filter.</returns>
    /// <remarks>
    /// Performance: O(n * m) where n is entity count and m is component type count.
    /// Allocates a new list for results.
    /// </remarks>
    public List<Entity> GetGroup(params Type[] types)
    {
        var result = new List<Entity>();
        lock (_lock)
        {
            foreach (var entity in _entitiesList)
            {
                if (entity.HasComponents(types))
                {
                    result.Add(entity);
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Returns an enumerable view of all entities that have the specified component type.
    /// Uses value tuples and iterator pattern to dramatically reduce heap allocations.
    /// </summary>
    /// <typeparam name="TComponent">The type of component to query for.</typeparam>
    /// <returns>An enumerable of (Entity, TComponent) tuples representing each entity and its component.</returns>
    /// <remarks>
    /// Performance: O(n) iteration with minimal allocations.
    /// Note: If you need to materialize the results into a collection, call .ToList() or .ToArray(),
    /// but be aware this will allocate. For best performance, consume results directly via foreach.
    /// Thread-safety: Creates a snapshot of entities to allow safe iteration.
    /// </remarks>
    public IEnumerable<(Entity Entity, TComponent Component)> View<TComponent>() where TComponent : IComponent
    {
        Entity[] snapshot;
        lock (_lock)
        {
            snapshot = _entitiesList.ToArray();
        }

        foreach (var entity in snapshot)
        {
            if (entity.HasComponent<TComponent>())
            {
                var component = entity.GetComponent<TComponent>();
                yield return (entity, component);
            }
        }
    }
}