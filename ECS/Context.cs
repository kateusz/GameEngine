namespace ECS;

/// <summary>
/// Manages the entity registry for a scene.
/// Thread-safe for concurrent access.
/// </summary>
public class Context : IContext
{
    public static event Action<Type>? ComponentIndexed;

    private readonly OrderedDictionary<int, Entity> _entities = new();
    private readonly Dictionary<Type, HashSet<Entity>> _entitiesByComponentType = new();
    private readonly Lock _lock = new();

    public void Register(Entity entity)
    {
        lock (_lock)
        {
            if (!_entities.TryAdd(entity.Id, entity))
                throw new InvalidOperationException($"Entity with ID {entity.Id} is already registered.");

            entity.ComponentAdded = componentType =>
            {
                lock (_lock)
                {
                    if (_entities.TryGetValue(entity.Id, out var registered) && ReferenceEquals(registered, entity))
                        IndexAdd(entity, componentType);
                }
                ComponentIndexed?.Invoke(componentType);
            };
            entity.ComponentRemoved = componentType =>
            {
                lock (_lock)
                {
                    if (_entities.TryGetValue(entity.Id, out var registered) && ReferenceEquals(registered, entity))
                        IndexRemove(entity, componentType);
                }
                ComponentIndexed?.Invoke(componentType);
            };
            IndexEntity(entity);
        }
    }

    public bool Remove(int entityId)
    {
        lock (_lock)
        {
            if (!_entities.Remove(entityId, out var entity))
                return false;

            entity.ClearComponentHooks();
            IndexRemoveEntity(entity, entity.ComponentTypes);
            return true;
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            foreach (var entity in _entities.Values)
                entity.ClearComponentHooks();

            _entities.Clear();
            _entitiesByComponentType.Clear();
        }
    }

    public Entity GetById(int entityId) => _entities[entityId];

    public Entity GetByName(string name) => _entities.Values.Single(e => e.Name == name);

    public IEnumerable<Entity> Entities
    {
        get
        {
            Entity[] snapshot;
            lock (_lock)
                snapshot = [.. _entities.Values];

            foreach (var entity in snapshot)
                yield return entity;
        }
    }

    public bool Contains(int entityId)
    {
        lock (_lock)
            return _entities.ContainsKey(entityId);
    }

    public ComponentView<TComponent> View<TComponent>() where TComponent : IComponent =>
        new(this);

    public DualComponentView<T1, T2> View<T1, T2>()
        where T1 : IComponent
        where T2 : IComponent =>
        new(this);

    internal void EnterViewIndex(
        Type primary,
        Type? secondary,
        out HashSet<Entity>.Enumerator enumerator,
        out bool empty)
    {
        _lock.Enter();
        empty = true;
        enumerator = default;

        var queryType = primary;
        if (secondary is not null)
        {
            var count1 = CountUnlocked(primary);
            var count2 = CountUnlocked(secondary);
            queryType = count1 <= count2 ? primary : secondary;
        }

        if (!_entitiesByComponentType.TryGetValue(queryType, out var entities) || entities.Count == 0)
            return;

        empty = false;
        enumerator = entities.GetEnumerator();
    }

    internal void ExitViewIndex() => _lock.Exit();

    private int CountUnlocked(Type componentType) =>
        _entitiesByComponentType.TryGetValue(componentType, out var entities) ? entities.Count : 0;

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
