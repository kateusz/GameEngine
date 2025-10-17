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

    /// <summary>
    /// Gets a component of the specified type from this entity.
    /// </summary>
    /// <typeparam name="T">The type of component to retrieve.</typeparam>
    /// <returns>The component instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the entity does not have the specified component.</exception>
    public T GetComponent<T>() where T : IComponent
    {
        if (_components.TryGetValue(typeof(T), out var component))
            return (T)component;

        throw new InvalidOperationException($"Entity {Id} ('{Name}') does not have component {typeof(T).Name}");
    }

    /// <summary>
    /// Attempts to get a component of the specified type from this entity.
    /// </summary>
    /// <typeparam name="T">The type of component to retrieve.</typeparam>
    /// <param name="component">When this method returns, contains the component if found; otherwise, the default value.</param>
    /// <returns>true if the component was found; otherwise, false.</returns>
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

    /// <summary>
    /// Determines whether two entities are equal based on their Id.
    /// </summary>
    /// <param name="left">The first entity to compare.</param>
    /// <param name="right">The second entity to compare.</param>
    /// <returns>true if both entities have the same Id; otherwise, false.</returns>
    public static bool operator ==(Entity? left, Entity? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two entities are not equal based on their Id.
    /// </summary>
    /// <param name="left">The first entity to compare.</param>
    /// <param name="right">The second entity to compare.</param>
    /// <returns>true if both entities have different Ids; otherwise, false.</returns>
    public static bool operator !=(Entity? left, Entity? right)
    {
        return !(left == right);
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