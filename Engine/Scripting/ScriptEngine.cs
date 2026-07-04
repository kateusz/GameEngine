using System.Reflection;
using Audio;
using CSharpFunctionalExtensions;
using ECS;
using Engine.Events;
using Engine.Events.Input;
using Engine.Scene;
using SceneComponents;
using Scripting;
using Serilog;
using ZLinq;

namespace Engine.Scripting;

internal sealed class ScriptEngine(
    ISceneContext sceneContext,
    IAudio audio,
    IAudioPlayback audioPlayback) : IScriptEngine
{
    private static readonly ILogger Logger = Log.ForContext<ScriptEngine>();

    private readonly Dictionary<string, Type> _scriptTypes = new();
    private readonly Dictionary<string, DateTime> _scriptLastModified = new();
    private Assembly? _dynamicAssembly;
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
            _dynamicAssembly = Assembly.LoadFrom(_loadedDllPath);
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

    public void OnUpdate(TimeSpan deltaTime, ScriptRuntimeStore store)
    {
        if (!_suppressFileChangeRecompile)
            CheckForScriptChanges();

        if (sceneContext.ActiveScene == null)
            return;

        var scriptEntities = sceneContext.ActiveScene.Entities
            .AsValueEnumerable()
            .Where(e => e.HasComponent<NativeScriptComponent>());

        foreach (var entity in scriptEntities)
        {
            var scriptComponent = entity.GetComponent<NativeScriptComponent>();
            var scriptableEntity = GetOrCreateRuntimeScript(store, entity, scriptComponent);
            if (scriptableEntity == null) continue;

            if (!scriptableEntity.IsInitialized)
            {
                scriptableEntity.SetEntity(entity);
                try
                {
                    scriptableEntity.OnCreate();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error initializing script on entity {EntityName}", entity.Name);
                }
            }

            try
            {
                scriptableEntity.OnUpdate(deltaTime);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error updating script on entity {EntityName}", entity.Name);
            }
        }
    }

    public void OnRuntimeStop(ScriptRuntimeStore store)
    {
        if (sceneContext.ActiveScene == null)
            return;

        var scriptEntities = sceneContext.ActiveScene.Entities
            .AsValueEnumerable()
            .Where(e => e.HasComponent<NativeScriptComponent>());

        var errorCount = 0;

        foreach (var entity in scriptEntities)
        {
            if (store.TryGet(entity.Id, out var scriptableEntity))
            {
                try
                {
                    scriptableEntity.OnDestroy();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error in script OnDestroy for entity '{EntityName}' (ID: {EntityId})", entity.Name, entity.Id);
                    errorCount++;
                }
            }
            store.Remove(entity.Id);
        }

        if (errorCount > 0)
        {
            Logger.Warning(
                "Scene stopped with {ErrorsCount} script error(s) during OnDestroy. Check logs above for details.",
                errorCount);
        }
    }

    public void ProcessEvent(Event @event, ScriptRuntimeStore store)
    {
        if (sceneContext.ActiveScene == null)
            return;

        var scriptEntities = sceneContext.ActiveScene.Entities
            .AsValueEnumerable()
            .Where(e => e.HasComponent<NativeScriptComponent>());

        foreach (var entity in scriptEntities)
        {
            if (!store.TryGet(entity.Id, out var scriptableEntity))
                continue;

            try
            {
                switch (@event)
                {
                    case KeyPressedEvent kpe:
                        scriptableEntity.OnKeyPressed(kpe.KeyCode);
                        break;
                    case KeyReleasedEvent kpe:
                        scriptableEntity.OnKeyReleased(kpe.KeyCode);
                        break;
                    case MouseButtonPressedEvent mbpe:
                        scriptableEntity.OnMouseButtonPressed(mbpe.Button);
                        break;
                    case MouseMovedEvent mme:
                        scriptableEntity.OnMouseMoved(mme.X, mme.Y);
                        break;
                    case MouseButtonReleasedEvent mbre:
                        scriptableEntity.OnMouseButtonReleased(mbre.Button);
                        break;
                    case MouseScrolledEvent mse:
                        scriptableEntity.OnMouseScrolled(mse.XOffSet, mse.YOffset);
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error processing event in script on entity {EntityName}", entity.Name);
            }
        }
    }

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

    public void RefreshScriptInstances(ScriptRuntimeStore store)
    {
        if (sceneContext.ActiveScene == null)
            return;

        var scriptEntities = sceneContext.ActiveScene.Entities
            .AsValueEnumerable()
            .Where(e => e.HasComponent<NativeScriptComponent>());

        foreach (var entity in scriptEntities)
        {
            var scriptComponent = entity.GetComponent<NativeScriptComponent>();
            if (string.IsNullOrWhiteSpace(scriptComponent.ScriptTypeName) || !_scriptTypes.ContainsKey(scriptComponent.ScriptTypeName))
                continue;

            var newInstance = CreateScriptInstance(scriptComponent.ScriptTypeName);
            if (!newInstance.IsSuccess)
                continue;

            store.Set(entity.Id, newInstance.Value);
            newInstance.Value.SetEntity(entity);
            newInstance.Value.OnCreate();
        }
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

    private ScriptableEntity? GetOrCreateRuntimeScript(ScriptRuntimeStore store, ECS.Entity entity, NativeScriptComponent scriptComponent)
    {
        if (store.TryGet(entity.Id, out var existing))
            return existing;

        if (string.IsNullOrWhiteSpace(scriptComponent.ScriptTypeName))
            return null;

        var result = CreateScriptInstance(scriptComponent.ScriptTypeName);
        if (!result.IsSuccess)
            return null;

        store.Set(entity.Id, result.Value);
        return result.Value;
    }
}
