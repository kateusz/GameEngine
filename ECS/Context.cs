namespace ECS;

public class Context
{
    public static event Action<Type>? ComponentIndexed;

    private static readonly HashSet<Entity> Empty = [];
    private readonly OrderedDictionary<int, Entity> _entities = new();
    private readonly Dictionary<Type, HashSet<Entity>> _entitiesByComponentType = new();

    public void Register(Entity entity)
    {
        if (!_entities.TryAdd(entity.Id, entity))
            throw new InvalidOperationException($"Entity with ID {entity.Id} is already registered.");

        entity.ComponentAdded = componentType =>
        {
            if (_entities.TryGetValue(entity.Id, out var registered) && ReferenceEquals(registered, entity))
                IndexAdd(entity, componentType);
            ComponentIndexed?.Invoke(componentType);
        };
        entity.ComponentRemoved = componentType =>
        {
            if (_entities.TryGetValue(entity.Id, out var registered) && ReferenceEquals(registered, entity))
                IndexRemove(entity, componentType);
            ComponentIndexed?.Invoke(componentType);
        };
        IndexEntity(entity);
    }

    public bool Remove(int entityId)
    {
        if (!_entities.Remove(entityId, out var entity))
            return false;

        entity.ClearComponentHooks();
        IndexRemoveEntity(entity, entity.ComponentTypes);
        return true;
    }

    public void Clear()
    {
        foreach (var entity in _entities.Values)
            entity.ClearComponentHooks();

        _entities.Clear();
        _entitiesByComponentType.Clear();
    }

    public Entity GetById(int entityId) => _entities[entityId];

    public Entity GetByName(string name) => _entities.Values.Single(e => e.Name == name);

    // ponytail: main-thread only; snapshot/lock if ECS is touched off the game loop.
    public IEnumerable<Entity> Entities => _entities.Values;

    public bool Contains(int entityId) => _entities.ContainsKey(entityId);

    public ComponentView<TComponent> View<TComponent>() where TComponent : IComponent =>
        new(Index(typeof(TComponent)));

    public DualComponentView<T1, T2> View<T1, T2>()
        where T1 : IComponent
        where T2 : IComponent
    {
        var a = Index(typeof(T1));
        var b = Index(typeof(T2));
        return new(a.Count <= b.Count ? a : b);
    }

    private HashSet<Entity> Index(Type type) =>
        _entitiesByComponentType.GetValueOrDefault(type) ?? Empty;

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
