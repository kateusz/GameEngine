using ECS;
using ECS.Systems;
using Engine.Scene;
using Engine.Scripting;
using Serilog;

namespace Engine.Scene.Systems;

internal sealed class ScriptUpdateSystem(
    IContext context,
    IScriptEngine scriptEngine,
    ScriptRuntimeStore scriptStore) : ISystem
{
    private static readonly ILogger Logger = Log.ForContext<ScriptUpdateSystem>();

    public int Priority => SystemPriorities.ScriptUpdateSystem;

    public void OnInit()
    {
        Logger.Debug("ScriptUpdateSystem initialized with priority {Priority}", Priority);
    }

    public void OnUpdate(TimeSpan deltaTime) =>
        NativeScriptIteration.Update(context, scriptEngine, scriptStore, deltaTime);

    public void OnShutdown()
    {
        Logger.Debug("ScriptUpdateSystem shutdown - destroying script instances");
        NativeScriptIteration.Shutdown(context, scriptStore);
        scriptStore.Clear();
    }
}
