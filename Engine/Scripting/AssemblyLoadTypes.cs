using System.Reflection;
using Engine.Core;

namespace Engine.Scripting;

[SkipUnitTests]
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
