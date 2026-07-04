using CSharpFunctionalExtensions;
using Engine.Events;
using Engine.Scene;
using Scripting;
using System.Reflection;

namespace Engine.Scripting;

public interface IScriptEngine
{
    void LoadGameAssemblyFromFile(string dllPath, string scriptsDirectory);

    void SetSuppressFileChangeRecompile(bool suppress);

    void OnUpdate(TimeSpan deltaTime, ScriptRuntimeStore store);

    void OnRuntimeStop(ScriptRuntimeStore store);

    void ProcessEvent(Event @event, ScriptRuntimeStore store);

    Type? GetScriptType(string scriptName);

    Result<ScriptableEntity> CreateScriptInstance(string scriptName);

    Assembly? GetLoadedGameAssembly();

    void RefreshScriptInstances(ScriptRuntimeStore store);
}
