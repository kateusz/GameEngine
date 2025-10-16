namespace ECS;

/// <summary>
/// Represents an entity in the Entity Component System.
/// Entities are uniquely identified by their Id property.
/// Equality comparisons are based solely on the immutable Id to ensure stable behavior in collections.
/// </summary>
public class Entity : IEquatable<Entity>
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

    /// <summary>
    /// Determines whether the specified object is an Entity with the same Id.
    /// </summary>
    /// <param name="obj">The object to compare with the current entity.</param>
    /// <returns>true if the specified object is an Entity with the same Id; otherwise, false.</returns>
    public override bool Equals(object? obj)
    {
        return obj is Entity other && Id == other.Id;
    }

    /// <summary>
    /// Determines whether the specified entity has the same Id as the current entity.
    /// </summary>
    /// <param name="other">The entity to compare with the current entity.</param>
    /// <returns>true if the specified entity has the same Id; otherwise, false.</returns>
    public bool Equals(Entity? other)
    {
        return other is not null && Id == other.Id;
    }

    /// <summary>
    /// Returns a hash code for this entity based on its immutable Id.
    /// The hash code remains stable throughout the entity's lifetime.
    /// </summary>
    /// <returns>A hash code for the current entity.</returns>
    public override int GetHashCode()
    {
        return Id.GetHashCode();
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