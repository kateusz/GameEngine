using CSharpFunctionalExtensions;
using ECS;
using Engine.Events;
using Engine.Scene;
using Scripting;
using System.Reflection;

namespace Engine.Scripting;

public interface IScriptEngine
{
    void LoadGameAssemblyFromFile(string dllPath, string scriptsDirectory);

    void SetSuppressFileChangeRecompile(bool suppress);

    void TryHotReload();

    void ProcessEvent(Event @event, IContext context, ScriptRuntimeStore store);

    Type? GetScriptType(string scriptName);

    Result<ScriptableEntity> CreateScriptInstance(string scriptName);

    Assembly? GetLoadedGameAssembly();

    void UnloadGameAssembly();
}
