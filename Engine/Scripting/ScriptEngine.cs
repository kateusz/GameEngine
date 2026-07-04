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
    private Assembly? _dynamicAssembly;
    private GameAssemblyLoadContext? _loadContext;

    public void LoadGameAssemblyFromFile(string dllPath)
    {
        var loadedDllPath = Path.GetFullPath(dllPath);
        if (!File.Exists(loadedDllPath))
        {
            Logger.Error("Game assembly not found: {Path}", loadedDllPath);
            return;
        }

        try
        {
            UnloadLoadContext();
            _loadContext = new GameAssemblyLoadContext(loadedDllPath);
            _dynamicAssembly = _loadContext.LoadAssembly();
            UpdateScriptTypes();
            Logger.Information("Loaded game assembly from {Path}", loadedDllPath);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load game assembly from {Path}", loadedDllPath);
        }
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

    public void UnloadGameAssembly() => UnloadLoadContext();

    private void UnloadLoadContext()
    {
        _dynamicAssembly = null;
        _scriptTypes.Clear();

        if (_loadContext is null)
            return;

        _loadContext.Unload();
        _loadContext = null;
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
