using System.Reflection;
using System.Runtime.Loader;
using Engine.Core;

namespace Engine.Scripting;

[SkipUnitTests]
internal sealed class GameAssemblyLoadContext(string assemblyPath) : AssemblyLoadContext(isCollectible: true)
{
    protected override Assembly? Load(AssemblyName assemblyName) => null;

    public Assembly LoadAssembly() => LoadFromAssemblyPath(assemblyPath);
}
