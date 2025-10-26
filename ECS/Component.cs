namespace ECS;

public interface IComponent
{
    /// <summary>
    /// Creates a deep copy of this component.
    /// </summary>
    /// <returns>A new instance with the same property values.</returns>
    IComponent Clone();
}

public abstract class Component : IComponent
{
    public abstract IComponent Clone();
}
