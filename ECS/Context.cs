namespace ECS;

/// <summary>
/// Manages the entity registry for a scene.
/// Thread-safe for concurrent access.
/// Refactored to support dependency injection - no longer a singleton.
/// </summary>
public class Context : IContext
{
    // Dual storage for optimal performance:
    // - Dictionary enables O(1) entity lookup and removal by ID
    // - List enables efficient iteration without boxing overhead
    private readonly Dictionary<int, Entity> _entitiesById = new();
    private readonly List<Entity> _entitiesList = [];
    private readonly Lock _lock = new();

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

    public Context()
    {
    }

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

    public void Clear()
    {
        lock (_lock)
        {
            _entitiesById.Clear();
            _entitiesList.Clear();
        }
    }

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