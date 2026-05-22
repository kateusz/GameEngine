namespace Scripting;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class RegisterAttribute(Type serviceType, GameIocLifetime lifetime = GameIocLifetime.Singleton) : Attribute
{
    public Type ServiceType { get; } = serviceType;

    public GameIocLifetime Lifetime { get; } = lifetime;
}
