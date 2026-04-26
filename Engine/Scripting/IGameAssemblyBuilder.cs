namespace Engine.Scripting;

public interface IGameAssemblyBuilder
{
    bool TryBuild(string scriptsDirectory, string outputDllPath, bool emitPdb, out string[] errors);
}
