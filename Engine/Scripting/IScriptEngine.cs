using System.Reflection;
using CSharpFunctionalExtensions;
using Engine.Events;
using Engine.Scene;

namespace Engine.Scripting;

/// <summary>
/// Interface for the script engine responsible for compiling, managing, and executing C# scripts at runtime.
/// Provides script compilation, hot-reloading, and lifecycle management for scriptable entities.
/// </summary>
public interface IScriptEngine
{
    /// <summary>
    /// Sets the scene manager for script execution.
    /// Required for dependency injection pattern - breaks circular dependency between ScriptEngine and SceneManager.
    /// </summary>
    /// <param name="sceneManager">The scene manager instance, or null to clear</param>
    void SetSceneManager(object? sceneManager);

    /// <summary>
    /// Sets the current scene for script execution.
    /// Required for dependency injection pattern - scripts need access to the active scene.
    /// </summary>
    /// <param name="scene">The active scene, or null to clear</param>
    void SetCurrentScene(IScene? scene);

    /// <summary>
    /// Sets the directory where script files (.cs) are located and triggers recompilation.
    /// </summary>
    /// <param name="scriptsDirectory">Path to the scripts directory</param>
    void SetScriptsDirectory(string scriptsDirectory);

    /// <summary>
    /// Updates all script components in the current scene.
    /// Checks for script file changes and triggers hot reload if needed.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last update</param>
    void OnUpdate(TimeSpan deltaTime);

    void OnRuntimeStop();

    /// <summary>
    /// Forwards events to all script components for processing.
    /// </summary>
    /// <param name="event">Event to process (keyboard, mouse, etc.)</param>
    void ProcessEvent(Event @event);

    /// <summary>
    /// Gets the names of all successfully compiled scripts.
    /// </summary>
    /// <returns>Array of script names</returns>
    string[] GetAvailableScriptNames();

    /// <summary>
    /// Gets the Type of a compiled script by name.
    /// </summary>
    /// <param name="scriptName">Name of the script class</param>
    /// <returns>Type of the script, or null if not found</returns>
    Type? GetScriptType(string scriptName);

    /// <summary>
    /// Gets the source code of a script by name.
    /// </summary>
    /// <param name="scriptName">Name of the script</param>
    /// <returns>Source code string, or empty if not found</returns>
    string GetScriptSource(string scriptName);

    /// <summary>
    /// Creates a new instance of a compiled script.
    /// </summary>
    /// <param name="scriptName">Name of the script class to instantiate</param>
    /// <returns>Result containing the ScriptableEntity instance or error message</returns>
    Result<ScriptableEntity> CreateScriptInstance(string scriptName);

    /// <summary>
    /// Creates a new script file or updates an existing one, then compiles it.
    /// </summary>
    /// <param name="scriptName">Name of the script (without .cs extension)</param>
    /// <param name="scriptContent">Source code content</param>
    /// <returns>Tuple indicating success and any compilation errors</returns>
    Task<(bool Success, string[] Errors)> CreateOrUpdateScriptAsync(string scriptName, string scriptContent);

    /// <summary>
    /// Deletes a script file and removes it from the compilation.
    /// </summary>
    /// <param name="scriptName">Name of the script to delete</param>
    /// <returns>True if successful, false otherwise</returns>
    bool DeleteScript(string scriptName);

    /// <summary>
    /// Enables or disables hybrid debugging (engine + scripts with PDB symbols).
    /// </summary>
    /// <param name="enable">True to enable debugging, false to disable</param>
    void EnableHybridDebugging(bool enable = true);

    /// <summary>
    /// Saves debug symbols (PDB file) to disk for external debugger use.
    /// </summary>
    /// <param name="outputPath">Path to save the PDB file</param>
    /// <param name="assemblyName">Name of the assembly (default: "DynamicScripts")</param>
    /// <returns>True if successful, false otherwise</returns>
    bool SaveDebugSymbols(string outputPath, string assemblyName = "DynamicScripts");

    /// <summary>
    /// Prints debug information about the script engine state to the logger.
    /// </summary>
    void PrintDebugInfo();

    /// <summary>
    /// Compiles all script files in the scripts directory.
    /// </summary>
    void CompileAllScripts();

    /// <summary>
    /// Forces recompilation of all scripts and reloads existing script instances.
    /// Used for hot-reloading during development.
    /// </summary>
    void ForceRecompile();
}
