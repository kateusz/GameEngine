namespace Engine.Scripting;

public sealed class GameAssemblyBuilder : IGameAssemblyBuilder
{
    public bool TryBuild(string scriptsDirectory, string outputDllPath, bool emitPdb, out string[] errors)
    {
        if (!GameAssemblyCompiler.TryCompile(scriptsDirectory, outputDllPath, emitPdb, useDebugOptimization: true, out var err))
        {
            errors = err ?? [];
            return false;
        }

        errors = [];
        return true;
    }
}
