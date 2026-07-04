using System.Reflection;

namespace Engine.Scripting;

internal static class AssemblyLoadTypes
{
    internal static IEnumerable<Type> From(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null).Cast<Type>().ToArray();
        }
    }
}
