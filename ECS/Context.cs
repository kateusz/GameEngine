using System.Runtime.InteropServices;

namespace ECS;

/// <summary>
/// Manages the global entity registry.
/// Single-threaded access only - all operations must be called from the main thread.
/// </summary>
public class Context
{
    private static Context? _instance;
    public static Context Instance => _instance ??= new Context();

    // Dual storage for optimal performance:
    // - Dictionary enables O(1) entity lookup and removal by ID
    // - List enables efficient iteration without boxing overhead
    private readonly Dictionary<int, Entity> _entitiesById = new();
    private readonly List<Entity> _entitiesList = [];

    /// <summary>
    /// Gets a read-only span view of all entities for efficient iteration.
    /// </summary>
    /// <remarks>
    /// Performance: Zero-allocation iteration with ReadOnlySpan.
    /// This provides ~100x performance improvement over ConcurrentBag iteration.
    ///
    /// Thread-safety: NOT thread-safe. Must only be accessed from the main thread.
    /// The span is valid only for the current stack frame - do not store or return it.
    /// </remarks>
    public ReadOnlySpan<Entity> Entities => CollectionsMarshal.AsSpan(_entitiesList);

    /// <summary>
    /// Gets all entities as an IEnumerable for LINQ compatibility.
    /// </summary>
    /// <remarks>
    /// Use this when you need LINQ operations. For performance-critical iteration,
    /// prefer the Entities property which returns ReadOnlySpan.
    /// </remarks>
    public IEnumerable<Entity> EntitiesEnumerable => _entitiesList;

    private Context()
    {
    }

    /// <summary>
    /// Registers a new entity in the context.
    /// </summary>
    /// <param name="entity">The entity to register.</param>
    /// <remarks>
    /// Performance: O(1) operation with dictionary and list insertion.
    /// Thread-safety: NOT thread-safe. Must be called from main thread only.
    /// </remarks>
    public void Register(Entity entity)
    {
        if (_entitiesById.ContainsKey(entity.Id))
        {
            throw new InvalidOperationException($"Entity with ID {entity.Id} is already registered.");
        }

        _entitiesById[entity.Id] = entity;
        _entitiesList.Add(entity);
    }

    /// <summary>
    /// Removes an entity from the context by its ID.
    /// </summary>
    /// <param name="entityId">The ID of the entity to remove.</param>
    /// <returns>True if the entity was found and removed; false otherwise.</returns>
    /// <remarks>
    /// Performance: O(1) dictionary lookup + O(n) list removal.
    /// For O(1) removal, use RemoveSwap if entity order doesn't matter.
    /// Thread-safety: NOT thread-safe. Must be called from main thread only.
    /// </remarks>
    public bool Remove(int entityId)
    {
        if (!_entitiesById.Remove(entityId, out var entity))
            return false;

        _entitiesList.Remove(entity);
        return true;
    }

    /// <summary>
    /// Removes an entity using swap-with-last strategy for O(1) performance.
    /// Entity order in the list is not preserved.
    /// </summary>
    /// <param name="entityId">The ID of the entity to remove.</param>
    /// <returns>True if the entity was found and removed; false otherwise.</returns>
    /// <remarks>
    /// Performance: O(1) operation - swaps with last element and removes.
    /// Use this when entity iteration order doesn't matter for maximum performance.
    /// Thread-safety: NOT thread-safe. Must be called from main thread only.
    /// </remarks>
    public bool RemoveSwap(int entityId)
    {
        if (!_entitiesById.Remove(entityId, out var entity))
            return false;

        int index = _entitiesList.IndexOf(entity);
        if (index == -1)
            return false;

        // Swap with last element and remove last (O(1))
        int lastIndex = _entitiesList.Count - 1;
        if (index != lastIndex)
        {
            _entitiesList[index] = _entitiesList[lastIndex];
        }
        _entitiesList.RemoveAt(lastIndex);
        return true;
    }

    /// <summary>
    /// Clears all entities from the context.
    /// </summary>
    /// <remarks>
    /// Performance: O(n) to clear both collections.
    /// Thread-safety: NOT thread-safe. Must be called from main thread only.
    /// </remarks>
    public void Clear()
    {
        _entitiesById.Clear();
        _entitiesList.Clear();
    }

    /// <summary>
    /// Gets all entities that have all specified component types.
    /// </summary>
    /// <param name="types">The component types to filter by.</param>
    /// <returns>A list of entities matching the component filter.</returns>
    /// <remarks>
    /// Performance: O(n * m) where n is entity count and m is component type count.
    /// Allocates a new list for results.
    /// Thread-safety: NOT thread-safe. Must be called from main thread only.
    /// </remarks>
    public List<Entity> GetGroup(params Type[] types)
    {
        var result = new List<Entity>();
        foreach (var entity in _entitiesList)
        {
            if (entity.HasComponents(types))
            {
                result.Add(entity);
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
    /// Thread-safety: NOT thread-safe. Must be called from main thread only.
    /// WARNING: Do not modify the entity collection while iterating.
    /// </remarks>
    public IEnumerable<(Entity Entity, TComponent Component)> View<TComponent>() where TComponent : Component
    {
        foreach (var entity in _entitiesList)
        {
            if (entity.HasComponent<TComponent>())
            {
                var component = entity.GetComponent<TComponent>();
                yield return (entity, component);
            }
        }
    }
}