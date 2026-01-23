namespace ECS;

/// <summary>
/// Represents an entity in the Entity Component System.
/// Entities are uniquely identified by their Id property.
/// Equality comparisons are based solely on the immutable Id to ensure stable behavior in collections.
/// </summary>
public class Entity : IEquatable<Entity>
{
    private readonly Dictionary<Type, IComponent> _components = new();
    
    public required int Id { get; init; }
    public required string Name { get; set; }

    private Entity()
    {
    }
    
    /// <summary>
    /// Validates that this entity does not already have a component of the specified type.
    /// </summary>
    /// <typeparam name="TComponent">The type of component to validate.</typeparam>
    /// <exception cref="InvalidOperationException">Thrown when the entity already has a component of the specified type.</exception>
    private void ValidateComponentNotExists<TComponent>() where TComponent : IComponent
    {
        if (_components.ContainsKey(typeof(TComponent)))
            throw new InvalidOperationException($"Entity {Id} ('{Name}') already has component {typeof(TComponent).Name}");
    }

    /// <summary>
    /// Adds a pre-constructed component instance to this entity with compile-time type safety.
    /// Use this overload when you want to initialize components with constructor parameters
    /// while maintaining strong typing and IntelliSense support.
    /// </summary>
    /// <typeparam name="TComponent">The compile-time type of component to add.</typeparam>
    /// <param name="component">The component instance to add.</param>
    /// <returns>The added component instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the entity already has a component of the specified type.</exception>
    /// <remarks>
    /// This method stores the component using the compile-time generic type parameter, not the runtime type.
    /// This ensures consistent behavior when working with component hierarchies.
    /// </remarks>
    public TComponent AddComponent<TComponent>(TComponent component) where TComponent : IComponent
    {
        ValidateComponentNotExists<TComponent>();
        _components[typeof(TComponent)] = component;
        return component;
    }

    /// <summary>
    /// Creates and adds a new component instance to this entity using a parameterless constructor.
    /// Use this overload when the component type has a parameterless constructor and you want
    /// to configure properties after creation.
    /// </summary>
    /// <typeparam name="TComponent">The type of component to create and add.</typeparam>
    /// <returns>The newly created component instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the entity already has a component of the specified type.</exception>
    public TComponent AddComponent<TComponent>() where TComponent : IComponent, new()
    {
        ValidateComponentNotExists<TComponent>();
        var component = new TComponent();
        _components[typeof(TComponent)] = component;
        return component;
    }
    
    /// <summary>
    /// Adds a component without compile-time type information.
    /// Used internally for cloning operations.
    /// </summary>
    public void AddComponentDynamic(IComponent component)
    {
        var componentType = component.GetType();
        if (!_components.TryAdd(componentType, component))
            throw new InvalidOperationException($"Entity {Id} ('{Name}') already has component {componentType.Name}");
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
    /// Gets all components attached to this entity.
    /// </summary>
    /// <returns>An enumerable collection of all components.</returns>
    public IEnumerable<IComponent> GetAllComponents()
    {
        return _components.Values;
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