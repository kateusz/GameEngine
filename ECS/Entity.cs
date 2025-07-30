namespace ECS;

public class Entity
{
    private readonly Dictionary<Type, IComponent> _components = new();
    
    public int Id { get; private set; }
    public required string Name { get; set; }
    
    public event Action<IComponent>? OnComponentAdded;

    public void AddComponent(IComponent component)
    {
        _components[component.GetType()] = component;
        OnComponentAdded?.Invoke(component);
    }

    public TComponent AddComponent<TComponent>() where TComponent : IComponent, new()
    {
        var component = new TComponent();
        _components[typeof(TComponent)] = component;
        OnComponentAdded?.Invoke(component);

        return component;
    }
    
    public void SetComponent<T>(T component) where T : IComponent
    {
        _components[typeof(T)] = component;
    }

    public void RemoveComponent<T>() where T : IComponent
    {
        _components.Remove(typeof(T));
    }

    public T GetComponent<T>() where T : IComponent
    {
        _components.TryGetValue(typeof(T), out IComponent component);
        return (T)component;
    }

    public bool HasComponent<T>() where T : IComponent
    {
        return _components.ContainsKey(typeof(T));
    }
    
    public bool HasComponents(Type[] componentTypes)
    {
        foreach (var type in componentTypes)
        {
            if (!_components.ContainsKey(type))
            {
                return false;
            }
        }
        return true;
    }

    public override bool Equals(object? obj)
    {
        return Id == ((Entity)obj).Id;
    }

    protected bool Equals(Entity other)
    {
        return _components.Equals(other._components) && Id.Equals(other.Id) && Name == other.Name;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_components, Id, Name);
    }

    public static Entity Create(int id, string name)
    {
        return new Entity
        {
            Id = id,
            Name = name,
        };
    }
}