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
    /// Priority 150 ensures scripts run after physics (typically 100) but before rendering (typically 200).
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
    /// </summary>
    public void OnShutdown()
    {
        Logger.Debug("ScriptUpdateSystem shutdown");
    }
}
