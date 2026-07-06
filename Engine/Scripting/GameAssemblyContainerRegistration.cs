using System.Reflection;
using DryIoc;
using Engine.Core;
using Scripting;

namespace Engine.Scripting;

[SkipUnitTests]
public static class GameAssemblyContainerRegistration
{
    public static void UnregisterRegistrationsFromGameAssembly(Container container, Assembly gameAssembly)
    {
        var gameAssemblyName = gameAssembly.GetName().Name;
        if (string.IsNullOrEmpty(gameAssemblyName))
            return;

        var toRemove = container.GetServiceRegistrations()
            .Where(r => TryGetImplementationType(r) is { } impl &&
                string.Equals(impl.Assembly.GetName().Name, gameAssemblyName, StringComparison.Ordinal))
            .ToList();

        foreach (var r in toRemove)
        {
            container.Unregister(
                r.ServiceType,
                r.OptionalServiceKey,
                FactoryType.Service,
                f => ReferenceEquals(f, r.Factory));
        }
    }

    private static Type? TryGetImplementationType(ServiceRegistrationInfo registration)
    {
        if (registration.Factory is null || !registration.Factory.CanAccessImplementationType)
            return null;

        return registration.Factory.ImplementationType;
    }

    public static bool TryRegisterContainer(Container container, Assembly assembly)
    {
        var items = DiscoverIocRegistrations(assembly);
        if (items.Count == 0)
            return false;

        UnregisterRegistrationsFromGameAssembly(container, assembly);
        foreach (var (implementationType, serviceType, lifetime) in items)
            Register(container, implementationType, serviceType, lifetime);
        return true;
    }

    private static List<(Type ImplementationType, Type ServiceType, GameIocLifetime Lifetime)> DiscoverIocRegistrations(Assembly assembly)
    {
        var list = new List<(Type ImplementationType, Type ServiceType, GameIocLifetime Lifetime)>();
        foreach (var type in AssemblyLoadTypes.From(assembly))
        {
            if (type is not { IsClass: true, IsAbstract: false })
                continue;

            var attr = type.GetCustomAttribute<RegisterAttribute>();
            if (attr is null)
                continue;

            if (attr.ServiceType.IsAssignableFrom(type))
            {
                list.Add((ImplementationType: type, ServiceType: attr.ServiceType, Lifetime: attr.Lifetime));
                continue;
            }

            throw new InvalidOperationException(
                $"Type {type.FullName} is marked with [Register] but does not implement {attr.ServiceType.FullName}.");
        }

        return list;
    }

    private static void Register(Container container, Type implementationType, Type serviceType, GameIocLifetime lifetime)
    {
        var reuse = lifetime switch
        {
            GameIocLifetime.Singleton => Reuse.Singleton,
            GameIocLifetime.Transient => Reuse.Transient,
            GameIocLifetime.Scoped => Reuse.Scoped,
            _ => throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null)
        };

        container.Register(serviceType, implementationType, reuse, ifAlreadyRegistered: IfAlreadyRegistered.Replace);
    }
}
