namespace ECS;

public class Entity
{
    public Guid Id { get; private set; }
    public string Name { get; set; }
    
    private readonly Dictionary<Type, Component> _components;
    public event Action<Component>? OnComponentAdded;

    public Entity(string name) : this(Guid.NewGuid(), name)
    {
    }

    public Entity(Guid id, string name)
    {
        Id = id;
        Name = name;
        _components = new Dictionary<Type, Component>();
    }

    public void AddComponent(Component component)
    {
        _components[component.GetType()] = component;
        OnComponentAdded?.Invoke(component);
    }

    public void AddComponent<TComponent>() where TComponent : Component, new()
    {
        var component = new TComponent();
        _components[typeof(TComponent)] = component;
        OnComponentAdded?.Invoke(component);
    }

    public void RemoveComponent<T>() where T : Component
    {
        _components.Remove(typeof(T));
    }

    public T GetComponent<T>() where T : Component
    {
        _components.TryGetValue(typeof(T), out Component component);
        return (T)component;
    }

    public bool HasComponent<T>() where T : Component
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
}