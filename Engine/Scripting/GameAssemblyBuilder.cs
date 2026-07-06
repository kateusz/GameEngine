using Engine.Core;

namespace Engine.Scripting;

[SkipUnitTests]
public sealed class GameAssemblyBuilder : IGameAssemblyBuilder
{
    public bool TryBuild(string scriptsDirectory, string outputDllPath, bool emitPdb, bool useDebugOptimization, out string[] errors)
    {
        if (!GameAssemblyCompiler.TryCompile(scriptsDirectory, outputDllPath, emitPdb, useDebugOptimization, out var err))
        {
            errors = err ?? [];
            return false;
        }

        errors = [];
        return true;
    }
}
