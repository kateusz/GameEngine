using System.Reflection;

namespace Engine.Scripting;

public interface IScriptEngine
{
    void LoadGameAssemblyFromFile(string dllPath);

    Assembly? GetLoadedGameAssembly();

    void UnloadGameAssembly();
}
