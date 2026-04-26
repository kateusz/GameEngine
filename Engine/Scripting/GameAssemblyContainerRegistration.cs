using System.Reflection;
using DryIoc;
using ZLinq;

namespace Engine.Scripting;

public static class GameAssemblyContainerRegistration
{
    /// <summary>
    /// Removes DryIoc factories whose implementation type comes from a compiled game assembly
    /// (assembly simple name <see cref="GameAssemblyCompiler.AssemblyName"/>), so a new DLL can
    /// call <c>IoCContainer.Register</c> again without duplicate-registration errors.
    /// </summary>
    public static void UnregisterRegistrationsFromGameAssemblyModules(Container container)
    {
        var gameAssemblyName = GameAssemblyCompiler.AssemblyName;
        var toRemove = container.GetServiceRegistrations()
            .Where(r => r.ImplementationType is { } impl &&
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

    public static Assembly Load(string assemblyNameOrFilePath)
    {
        if (string.IsNullOrWhiteSpace(assemblyNameOrFilePath))
            throw new ArgumentException("Assembly name or path is required.", nameof(assemblyNameOrFilePath));
        if (assemblyNameOrFilePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
            File.Exists(assemblyNameOrFilePath))
            return Assembly.LoadFrom(Path.GetFullPath(assemblyNameOrFilePath));
        return Assembly.Load(assemblyNameOrFilePath);
    }

    public static bool TryRegisterContainer(Container container, Assembly assembly)
    {
        var registerMethod = FindRegisterMethod(assembly);
        if (registerMethod is null)
            return false;
        var parameterType = registerMethod.GetParameters().Single().ParameterType;
        if (parameterType == typeof(IRegistrator) || parameterType == typeof(Container))
        {
            UnregisterRegistrationsFromGameAssemblyModules(container);
            registerMethod.Invoke(null, [container]);
            return true;
        }

        throw new InvalidOperationException(
            $"Unsupported game registration signature: Register({parameterType.Name})");
    }

    private static MethodInfo? FindRegisterMethod(Assembly assembly)
    {
        var name = assembly.GetName().Name ?? "";
        var expectedType = assembly.GetType($"{name}.IoCContainer");
        if (expectedType != null)
        {
            var m = GetRegisterMethod(expectedType);
            if (m is not null)
                return m;
        }

        return assembly.GetTypes()
            .AsValueEnumerable()
            .Where(t => string.Equals(t.Name, "IoCContainer", StringComparison.Ordinal))
            .Select(GetRegisterMethod)
            .FirstOrDefault(m => m is not null);
    }

    private static MethodInfo? GetRegisterMethod(Type type)
    {
        return type.GetMethod(
                   "Register",
                   BindingFlags.Public | BindingFlags.Static,
                   binder: null,
                   types: [typeof(IRegistrator)],
                   modifiers: null)
               ?? type.GetMethod(
                   "Register",
                   BindingFlags.Public | BindingFlags.Static,
                   binder: null,
                   types: [typeof(Container)],
                   modifiers: null);
    }
}
