namespace ECS;

public sealed class Entity(int id, string name)
{
    private readonly Dictionary<Type, IComponent> _components = new();

    public int Id { get; } = id;
    public string Name { get; set; } = name;

    public TComponent AddComponent<TComponent>() where TComponent : IComponent, new()
        => AddComponent(new TComponent());

    public TComponent AddComponent<TComponent>(TComponent component) where TComponent : IComponent
    {
        Add(typeof(TComponent), component);
        return component;
    }

    public void AddComponentDynamic(IComponent component) => Add(component.GetType(), component);

    private void Add(Type type, IComponent component)
    {
        if (!_components.TryAdd(type, component))
            throw new InvalidOperationException($"Entity {Id} ('{Name}') already has component {type.Name}");
        ComponentAdded?.Invoke(type);
    }

    public void RemoveComponent<T>() where T : IComponent => RemoveComponent(typeof(T));

    public void RemoveComponent(Type componentType)
    {
        if (_components.Remove(componentType))
            ComponentRemoved?.Invoke(componentType);
    }

    public T GetComponent<T>() where T : IComponent
        => TryGetComponent<T>(out var component)
            ? component
            : throw new InvalidOperationException($"Entity {Id} ('{Name}') does not have component {typeof(T).Name}");

    public bool TryGetComponent<T>(out T component) where T : IComponent
    {
        if (_components.TryGetValue(typeof(T), out var comp))
        {
            component = (T)comp;
            return true;
        }

        component = default!;
        return false;
    }

    public bool TryGetComponent(Type componentType, out IComponent? component)
        => _components.TryGetValue(componentType, out component);

    public bool HasComponent<T>() where T : IComponent => _components.ContainsKey(typeof(T));

    public IEnumerable<IComponent> GetAllComponents() => _components.Values;

    internal IEnumerable<Type> ComponentTypes => _components.Keys;

    internal Action<Type>? ComponentAdded;
    internal Action<Type>? ComponentRemoved;

    internal void ClearComponentHooks()
    {
        ComponentAdded = null;
        ComponentRemoved = null;
    }
}
