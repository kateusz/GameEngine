using ECS;
using Engine.Scene.Components;
using Engine.Scripting;
using Serilog;

namespace Engine.Scene.Systems;

/// <summary>
/// System responsible for updating all script components in the scene.
/// Operates on entities with NativeScriptComponent.
/// </summary>
public class ScriptUpdateSystem : ISystem
{
    private static readonly ILogger Logger = Log.ForContext<ScriptUpdateSystem>();

    /// <summary>
    /// Gets the priority of this system.
    /// Priority 150 ensures scripts run before physics (which executes inline after system updates)
    /// and before rendering (typically 200). Execution order: scripts → physics → rendering.
    /// </summary>
    public int Priority => 150;

    /// <summary>
    /// Called once when the system is initialized.
    /// </summary>
    public void OnInit()
    {
        Logger.Debug("ScriptUpdateSystem initialized with priority {Priority}", Priority);
    }

    /// <summary>
    /// Called every frame to update all script components.
    /// Delegates to ScriptEngine which handles hot-reload and script lifecycle.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update.</param>
    public void OnUpdate(TimeSpan deltaTime)
    {
        // Delegate to ScriptEngine which handles:
        // - Hot-reload detection
        // - Script initialization (OnCreate)
        // - Script updates (OnUpdate)
        // - Error handling and logging
        ScriptEngine.Instance.OnUpdate(deltaTime);
    }

    /// <summary>
    /// Called when the system is being shut down.
    /// Delegates to ScriptEngine to handle OnDestroy lifecycle for all scripts.
    /// </summary>
    public void OnShutdown()
    {
        Logger.Debug("ScriptUpdateSystem shutdown - calling OnRuntimeStop for all scripts");

        // Delegate to ScriptEngine which handles:
        // - Script destruction (OnDestroy)
        // - Error handling and logging
        ScriptEngine.Instance.OnRuntimeStop();
    }
}
