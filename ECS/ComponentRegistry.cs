namespace ECS;

/// <summary>
/// Provides a centralized registry for component cloning operations.
/// Supports both registered custom cloners and fallback shallow copying.
/// </summary>
public static class ComponentRegistry
{
    private static readonly Dictionary<Type, Func<IComponent, IComponent>> _cloners = new();

    /// <summary>
    /// Registers a custom cloning function for a specific component type.
    /// </summary>
    /// <typeparam name="T">The component type to register.</typeparam>
    /// <param name="cloner">Function that creates a deep copy of the component.</param>
    public static void RegisterCloner<T>(Func<T, T> cloner) where T : IComponent
    {
        _cloners[typeof(T)] = component => cloner((T)component);
    }

    /// <summary>
    /// Clones a component instance using either a registered cloner or shallow copy fallback.
    /// </summary>
    /// <param name="component">The component to clone.</param>
    /// <returns>A cloned instance of the component.</returns>
    /// <exception cref="InvalidOperationException">Thrown when cloning fails.</exception>
    public static IComponent Clone(IComponent component)
    {
        var type = component.GetType();

        // Use registered cloner if available
        if (_cloners.TryGetValue(type, out var cloner))
            return cloner(component);

        // Fallback to MemberwiseClone for simple components
        // This creates a shallow copy, which is sufficient for value-type properties
        var method = type.GetMethod("MemberwiseClone",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        if (method != null)
        {
            var clonedComponent = method.Invoke(component, null);
            if (clonedComponent != null)
                return (IComponent)clonedComponent;
        }

        throw new InvalidOperationException(
            $"No cloner registered for {type.Name} and MemberwiseClone failed. " +
            $"Register a custom cloner using ComponentRegistry.RegisterCloner<{type.Name}>()");
    }
}
