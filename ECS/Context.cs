namespace ECS;

/// <summary>
/// Manages the entity registry for a scene.
/// Thread-safe for concurrent access.
/// </summary>
public class Context : IContext
{
    private readonly Dictionary<int, Entity> _entitiesById = new();
    private readonly List<Entity> _entitiesList = [];
    private readonly Dictionary<Type, HashSet<Entity>> _entitiesByComponentType = new();
    private readonly Lock _lock = new();

    public void Register(Entity entity)
    {
        lock (_lock)
        {
            if (!_entitiesById.TryAdd(entity.Id, entity))
                throw new InvalidOperationException($"Entity with ID {entity.Id} is already registered.");

            _entitiesList.Add(entity);
            entity.ComponentAdded = componentType => IndexAdd(entity, componentType);
            entity.ComponentRemoved = componentType => IndexRemove(entity, componentType);
            IndexEntity(entity);
        }
    }

    public bool Remove(int entityId)
    {
        lock (_lock)
        {
            if (!_entitiesById.Remove(entityId, out var entity))
                return false;

            _entitiesList.Remove(entity);
            entity.ClearComponentHooks();
            IndexRemoveEntity(entity, entity.ComponentTypes);
            return true;
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            foreach (var entity in _entitiesList)
                entity.ClearComponentHooks();

            _entitiesById.Clear();
            _entitiesList.Clear();
            _entitiesByComponentType.Clear();
        }
    }

    public Entity GetById(int entityId) => _entitiesById[entityId];

    public Entity GetByName(string name) => _entitiesList.FirstOrDefault(e => e.Name == name);

    public IEnumerable<(Entity Entity, TComponent Component)> View<TComponent>() where TComponent : IComponent
    {
        var snapshot = Snapshot<TComponent>();
        foreach (var entity in snapshot)
        {
            if (entity.TryGetComponent<TComponent>(out var component))
                yield return (entity, component);
        }
    }

    public IEnumerable<(Entity Entity, T1 Component1, T2 Component2)> View<T1, T2>()
        where T1 : IComponent
        where T2 : IComponent
    {
        Entity[] snapshot;
        lock (_lock)
        {
            snapshot = Count<T1>() <= Count<T2>()
                ? SnapshotUnlocked<T1>()
                : SnapshotUnlocked<T2>();
        }

        foreach (var entity in snapshot)
        {
            if (!entity.TryGetComponent<T1>(out var component1) || !entity.TryGetComponent<T2>(out var component2))
                continue;

            yield return (entity, component1, component2);
        }
    }

    private Entity[] Snapshot<TComponent>() where TComponent : IComponent
    {
        lock (_lock)
            return SnapshotUnlocked<TComponent>();
    }

    private Entity[] SnapshotUnlocked<TComponent>() where TComponent : IComponent
    {
        if (!_entitiesByComponentType.TryGetValue(typeof(TComponent), out var entities) || entities.Count == 0)
            return [];

        var snapshot = new Entity[entities.Count];
        entities.CopyTo(snapshot);
        return snapshot;
    }

    private int Count<TComponent>() where TComponent : IComponent =>
        _entitiesByComponentType.TryGetValue(typeof(TComponent), out var entities) ? entities.Count : 0;

    private void IndexAdd(Entity entity, Type componentType)
    {
        if (!_entitiesByComponentType.TryGetValue(componentType, out var entities))
        {
            entities = [];
            _entitiesByComponentType[componentType] = entities;
        }

        entities.Add(entity);
    }

    private void IndexRemove(Entity entity, Type componentType)
    {
        if (!_entitiesByComponentType.TryGetValue(componentType, out var entities))
            return;

        entities.Remove(entity);
        if (entities.Count == 0)
            _entitiesByComponentType.Remove(componentType);
    }

    private void IndexRemoveEntity(Entity entity, IEnumerable<Type> componentTypes)
    {
        foreach (var componentType in componentTypes)
            IndexRemove(entity, componentType);
    }

    private void IndexEntity(Entity entity)
    {
        foreach (var componentType in entity.ComponentTypes)
            IndexAdd(entity, componentType);
    }
}
