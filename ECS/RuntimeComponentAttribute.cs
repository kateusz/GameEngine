namespace ECS;

/// <summary>
/// Marks an <see cref="IComponent"/> as engine/runtime-only. Scene save skips these types.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class RuntimeComponentAttribute : Attribute;
