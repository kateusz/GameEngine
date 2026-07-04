namespace Engine.Scripting;

public interface IGameAssemblyBuilder
{
    bool TryBuild(string scriptsDirectory, string outputDllPath, bool emitPdb, bool useDebugOptimization, out string[] errors);
}
