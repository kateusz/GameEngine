namespace ECS;

/// <summary>
/// Manages the entity registry for a scene.
/// Thread-safe for concurrent access.
/// </summary>
public class Context : IContext
{
    private readonly Dictionary<int, Entity> _entitiesById = new();
    
    // - List enables efficient iteration without boxing overhead
    private readonly List<Entity> _entitiesList = [];
    private readonly Lock _lock = new();

    public Context()
    {
    }

    public void Register(Entity entity)
    {
        lock (_lock)
        {
            if (!_entitiesById.TryAdd(entity.Id, entity))
            {
                throw new InvalidOperationException($"Entity with ID {entity.Id} is already registered.");
            }

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
    
    public Entity GetById(int entityId) => _entitiesById[entityId];
    
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