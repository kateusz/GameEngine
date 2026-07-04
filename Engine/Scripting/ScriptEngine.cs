using System.Reflection;
using Audio;
using CSharpFunctionalExtensions;
using ECS;
using Engine.Events;
using Engine.Scene;
using Engine.Scene.Systems;
using Scripting;
using Serilog;

namespace Engine.Scripting;

internal sealed class ScriptEngine(IAudio audio, IAudioPlayback audioPlayback) : IScriptEngine
{
    private static readonly ILogger Logger = Log.ForContext<ScriptEngine>();

    private readonly Dictionary<string, Type> _scriptTypes = new();
    private readonly Dictionary<string, DateTime> _scriptLastModified = new();
    private Assembly? _dynamicAssembly;
    private GameAssemblyLoadContext? _loadContext;
    private string _scriptsDirectory = Path.Combine(Environment.CurrentDirectory, "assets", "scripts");
    private string? _loadedDllPath;
    private bool _suppressFileChangeRecompile;

    public void LoadGameAssemblyFromFile(string dllPath, string scriptsDirectory)
    {
        _scriptsDirectory = scriptsDirectory;
        Directory.CreateDirectory(_scriptsDirectory);
        _loadedDllPath = Path.GetFullPath(dllPath);
        if (!File.Exists(_loadedDllPath))
        {
            Logger.Error("Game assembly not found: {Path}", _loadedDllPath);
            return;
        }

        try
        {
            UnloadLoadContext();
            _loadContext = new GameAssemblyLoadContext(_loadedDllPath);
            _dynamicAssembly = _loadContext.LoadAssembly();
            IndexScriptLastModifiedFromDisk();
            UpdateScriptTypes();
            Logger.Information("Loaded game assembly from {Path}", _loadedDllPath);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load game assembly from {Path}", _loadedDllPath);
        }
    }

    public void SetSuppressFileChangeRecompile(bool suppress) => _suppressFileChangeRecompile = suppress;

    public void TryHotReload()
    {
        if (!_suppressFileChangeRecompile)
            CheckForScriptChanges();
    }

    public void ProcessEvent(Event @event, IContext context, ScriptRuntimeStore store) =>
        NativeScriptIteration.ProcessEvent(context, store, @event);

    public Type? GetScriptType(string scriptName) => _scriptTypes.TryGetValue(scriptName, out var type) ? type : null;

    public Result<ScriptableEntity> CreateScriptInstance(string scriptName)
    {
        if (!_scriptTypes.TryGetValue(scriptName, out var scriptType))
        {
            var error = $"Script type '{scriptName}' not found";
            Logger.Error(error);
            return Result.Failure<ScriptableEntity>(error);
        }

        try
        {
            var componentAccessor = new ComponentAccessor();
            return Activator.CreateInstance(scriptType, componentAccessor, audio, audioPlayback) is ScriptableEntity instance
                ? Result.Success(instance)
                : Result.Failure<ScriptableEntity>($"Unable to create instance of {scriptType}");
        }
        catch (Exception ex)
        {
            var error = $"Failed to create instance of script '{scriptName}'";
            Logger.Error(ex, error);
            return Result.Failure<ScriptableEntity>(error);
        }
    }

    public Assembly? GetLoadedGameAssembly() => _dynamicAssembly;

    public void UnloadGameAssembly()
    {
        UnloadLoadContext();
        _loadedDllPath = null;
        _scriptLastModified.Clear();
    }

    private void UnloadLoadContext()
    {
        _dynamicAssembly = null;
        _scriptTypes.Clear();

        if (_loadContext is null)
            return;

        _loadContext.Unload();
        _loadContext = null;
    }

    private void CheckForScriptChanges()
    {
        if (string.IsNullOrEmpty(_loadedDllPath))
            return;

        var needsRecompile = false;

        foreach (var (scriptName, lastModified) in _scriptLastModified)
        {
            var scriptPath = Path.Combine(_scriptsDirectory, $"{scriptName}.cs");
            if (!File.Exists(scriptPath))
                continue;

            if (File.GetLastWriteTime(scriptPath) > lastModified)
            {
                needsRecompile = true;
                break;
            }
        }

        if (!needsRecompile)
            return;

        Logger.Information("Script changes detected, recompiling...");
        if (!GameAssemblyCompiler.TryCompile(_scriptsDirectory, _loadedDllPath, emitPdb: false, useDebugOptimization: false, out var errors))
        {
            foreach (var err in errors ?? [])
                Logger.Error("Script hot-reload: {Error}", err);
            return;
        }

        LoadGameAssemblyFromFile(_loadedDllPath, _scriptsDirectory);
    }

    private void IndexScriptLastModifiedFromDisk()
    {
        _scriptLastModified.Clear();
        if (!Directory.Exists(_scriptsDirectory))
            return;

        foreach (var scriptPath in GameAssemblyCompiler.EnumerateGameScriptFiles(_scriptsDirectory))
        {
            var scriptName = Path.GetFileNameWithoutExtension(scriptPath);
            try
            {
                _scriptLastModified[scriptName] = File.GetLastWriteTime(scriptPath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to index script file {Path}", scriptPath);
            }
        }
    }

    private void UpdateScriptTypes()
    {
        _scriptTypes.Clear();

        if (_dynamicAssembly == null)
            return;

        foreach (var type in _dynamicAssembly.GetTypes())
        {
            if (typeof(ScriptableEntity).IsAssignableFrom(type) && !type.IsAbstract)
            {
                _scriptTypes[type.Name] = type;
                Logger.Debug("Registered script type: {TypeName}", type.Name);
            }
        }
    }
}
