namespace ECS;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class SerializableComponentAttribute(string? name = null) : Attribute
{
    public string? Name { get; } = name;
}
