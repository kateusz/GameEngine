using System.Reflection;
using CSharpFunctionalExtensions;
using Engine.Core.Input;
using Engine.Events;
using Engine.Events.Input;
using Engine.Scene;
using Engine.Scene.Components;
using Serilog;
using ZLinq;

namespace Engine.Scripting;

internal sealed class ScriptEngine : IScriptEngine
{
    private static readonly ILogger Logger = Log.ForContext<ScriptEngine>();

    private readonly Dictionary<string, Type> _scriptTypes = new();
    private readonly Dictionary<string, DateTime> _scriptLastModified = new();
    private readonly Dictionary<string, string> _scriptSources = new();
    private readonly ISceneContext _sceneContext;
    private string _scriptsDirectory;
    private Assembly? _dynamicAssembly;

    private readonly Dictionary<string, byte[]> _debugSymbols = new();
    private bool _debugMode = true;
    private bool _suppressFileChangeRecompile;

    public ScriptEngine(ISceneContext sceneContext)
    {
        _sceneContext = sceneContext;
        _scriptsDirectory = Path.Combine(Environment.CurrentDirectory, "assets", "scripts");
        Directory.CreateDirectory(_scriptsDirectory);
    }

    public void SetScriptsDirectory(string scriptsDirectory)
    {
        _scriptsDirectory = scriptsDirectory;
        Directory.CreateDirectory(_scriptsDirectory);
        _suppressFileChangeRecompile = false;
        CompileAllScripts();
    }

    public void LoadGameAssemblyFromFile(string dllPath, string scriptsDirectory)
    {
        _scriptsDirectory = scriptsDirectory;
        Directory.CreateDirectory(_scriptsDirectory);
        var full = Path.GetFullPath(dllPath);
        if (!File.Exists(full))
        {
            Logger.Error("Game assembly not found: {Path}", full);
            return;
        }

        try
        {
            _dynamicAssembly = Assembly.LoadFrom(full);
            if (_debugMode && File.Exists(Path.ChangeExtension(full, ".pdb")))
                _debugSymbols[GameAssemblyCompiler.AssemblyName] = File.ReadAllBytes(Path.ChangeExtension(full, ".pdb")!);

            IndexScriptSourcesFromDisk();
            UpdateScriptTypes();
            Logger.Information("Loaded game assembly from {Path}", full);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load game assembly from {Path}", full);
        }
    }

    public void SetSuppressFileChangeRecompile(bool suppress) => _suppressFileChangeRecompile = suppress;

    public void OnUpdate(TimeSpan deltaTime)
    {
        if (!_suppressFileChangeRecompile)
            CheckForScriptChanges();

        // TODO: check if ActiveScene could be null
        if (_sceneContext.ActiveScene == null)
            return;

        var scriptEntities = _sceneContext.ActiveScene.Entities
            .AsValueEnumerable()
            .Where(e => e.HasComponent<NativeScriptComponent>());

        foreach (var entity in scriptEntities)
        {
            var scriptComponent = entity.GetComponent<NativeScriptComponent>();
            if (scriptComponent.ScriptableEntity == null) continue;

            if (scriptComponent.ScriptableEntity.Entity == null)
            {
                scriptComponent.ScriptableEntity.SetEntity(entity);
                scriptComponent.ScriptableEntity.SetSceneContext(_sceneContext);
                try
                {
                    scriptComponent.ScriptableEntity.OnCreate();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error initializing script on entity {EntityName}", entity.Name);
                }
            }

            try
            {
                scriptComponent.ScriptableEntity.OnUpdate(deltaTime);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error updating script on entity {EntityName}", entity.Name);
            }
        }
    }

    public void OnRuntimeStop()
    {
        if (_sceneContext.ActiveScene == null)
            return;

        var scriptEntities = _sceneContext.ActiveScene.Entities
            .AsValueEnumerable()
            .Where(e => e.HasComponent<NativeScriptComponent>());

        var errorCount = 0;

        foreach (var entity in scriptEntities)
        {
            var scriptComponent = entity.GetComponent<NativeScriptComponent>();
            if (scriptComponent.ScriptableEntity != null)
            {
                try
                {
                    scriptComponent.ScriptableEntity.OnDestroy();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error in script OnDestroy for entity '{EntityName}' (ID: {EntityId})", entity.Name, entity.Id);
                    errorCount++;
                }
            }
        }

        if (errorCount > 0)
        {
            Logger.Warning(
                "Scene stopped with {ErrorsCount} script error(s) during OnDestroy. Check logs above for details.",
                errorCount);
        }
    }

    public void ProcessEvent(Event @event)
    {
        if (_sceneContext.ActiveScene == null)
            return;

        var scriptEntities = _sceneContext.ActiveScene.Entities
            .AsValueEnumerable()
            .Where(e => e.HasComponent<NativeScriptComponent>());

        foreach (var entity in scriptEntities)
        {
            var scriptComponent = entity.GetComponent<NativeScriptComponent>();
            if (scriptComponent.ScriptableEntity == null) 
                continue;

            try
            {
                switch (@event)
                {
                    case KeyPressedEvent kpe:
                        scriptComponent.ScriptableEntity.OnKeyPressed(kpe.KeyCode);
                        break;
                    case KeyReleasedEvent kpe:
                        scriptComponent.ScriptableEntity.OnKeyReleased(kpe.KeyCode);
                        break;
                    case MouseButtonPressedEvent mbpe:
                        scriptComponent.ScriptableEntity.OnMouseButtonPressed(mbpe.Button);
                        break;
                    case MouseMovedEvent mme:
                        scriptComponent.ScriptableEntity.OnMouseMoved(mme.X, mme.Y);
                        break;
                    case MouseButtonReleasedEvent mbre:
                        scriptComponent.ScriptableEntity.OnMouseButtonReleased(mbre.Button);
                        break;
                    case MouseScrolledEvent mse:
                        scriptComponent.ScriptableEntity.OnMouseScrolled(mse.XOffSet, mse.YOffset);
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error processing event in script on entity {EntityName}", entity.Name);
            }
        }
    }

    public string[] GetAvailableScriptNames() => _scriptTypes.Keys.ToArray();

    public Type? GetScriptType(string scriptName) => _scriptTypes.TryGetValue(scriptName, out var type) ? type : null;

    public string GetScriptSource(string scriptName)
    {
        if (_scriptSources.TryGetValue(scriptName, out var source))
            return source;

        var scriptPath = Path.Combine(_scriptsDirectory, $"{scriptName}.cs");
        if (!File.Exists(scriptPath)) 
            return string.Empty;
        
        var src = File.ReadAllText(scriptPath);
        _scriptSources[scriptName] = src;
        return src;

    }

    public string? GetScriptFilePath(string scriptName)
    {
        var scriptPath = Path.Combine(_scriptsDirectory, $"{scriptName}.cs");
        return File.Exists(scriptPath) ? scriptPath : null;
    }

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
            var instance = Activator.CreateInstance(scriptType) as ScriptableEntity;
            return instance is null
                ? Result.Failure<ScriptableEntity>($"Unable to create instance of {scriptType}")
                : Result.Success(instance);
        }
        catch (Exception ex)
        {
            var error = $"Failed to create instance of script '{scriptName}'";
            Logger.Error(ex, error);
            return Result.Failure<ScriptableEntity>(error);
        }
    }

    public async Task<(bool Success, string[] Errors)> CreateOrUpdateScriptAsync(string scriptName,
        string scriptContent)
    {
        var scriptPath = Path.Combine(_scriptsDirectory, $"{scriptName}.cs");

        try
        {
            await File.WriteAllTextAsync(scriptPath, scriptContent);
            _scriptSources[scriptName] = scriptContent;
            _scriptLastModified[scriptName] = File.GetLastWriteTime(scriptPath);
            
            var (success, errors) = CompileScript(scriptName, scriptContent);
            if (success)
            {
                Logger.Information("Script '{ScriptName}' successfully compiled", scriptName);
                return (true, []);
            }

            Logger.Error("Failed to compile script '{ScriptName}': {Errors}", scriptName, string.Join(", ", errors));
            return (false, errors);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error saving or compiling script '{ScriptName}'", scriptName);
            return (false, [ex.Message]);
        }
    }

    public bool DeleteScript(string scriptName)
    {
        var scriptPath = Path.Combine(_scriptsDirectory, $"{scriptName}.cs");

        try
        {
            if (File.Exists(scriptPath)) 
                File.Delete(scriptPath);

            _scriptTypes.Remove(scriptName);
            _scriptSources.Remove(scriptName);
            _scriptLastModified.Remove(scriptName);
            
            CompileAllScripts();

            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error deleting script '{ScriptName}'", scriptName);
            return false;
        }
    }

    public void EnableHybridDebugging(bool enable = true)
    {
        _debugMode = enable;

        if (enable)
        {
            Logger.Information("Hybrid debugging enabled - engine + scripts");
            CompileAllScripts();
        }
    }
    
    public bool SaveDebugSymbols(string outputPath, string assemblyName = "GameAssembly")
    {
        try
        {
            if (_debugSymbols.TryGetValue(assemblyName, out var symbols))
            {
                File.WriteAllBytes($"{outputPath}.pdb", symbols);

                // Also save the assembly for complete debugging setup
                if (_dynamicAssembly != null && !string.IsNullOrEmpty(_dynamicAssembly.Location))
                {
                    File.Copy(_dynamicAssembly.Location, $"{outputPath}.dll", true);
                }

                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to save debug symbols to {OutputPath}", outputPath);
            return false;
        }
    }
    
    public void PrintDebugInfo()
    {
        Logger.Debug("=== SCRIPT ENGINE DEBUG INFO === DebugMode: {DebugMode}, ScriptsDirectory: {ScriptsDirectory}, Loaded Scripts: {ScriptCount}",
            _debugMode, _scriptsDirectory, _scriptTypes.Count);

        foreach (var (name, type) in _scriptTypes)
        {
            Logger.Debug("  - {ScriptName}: {TypeFullName}", name, type.FullName);
        }

        Logger.Debug("Debug Symbols Available: {DebugSymbolsAvailable}", _debugSymbols.Count > 0);

        if (_dynamicAssembly != null)
        {
            Logger.Debug("Assembly Location: {AssemblyLocation}", _dynamicAssembly.Location);
            Logger.Debug("Assembly Full Name: {AssemblyFullName}", _dynamicAssembly.FullName);
        }

        Logger.Debug("================================");
    }
    
    public void CompileAllScripts()
    {
        Logger.Information("Compiling all scripts to {GameAssembly}...", GameAssemblyCompiler.AssemblyName);
        if (!Directory.Exists(_scriptsDirectory))
        {
            Logger.Warning("Scripts directory does not exist: {Dir}", _scriptsDirectory);
            return;
        }

        var outputPath = AllocateGameAssemblyBuildPath();
        if (!GameAssemblyCompiler.TryCompile(
                _scriptsDirectory,
                outputPath,
                _debugMode,
                useDebugOptimization: _debugMode,
                out var errors))
        {
            foreach (var err in errors ?? [])
                Logger.Error("Script compilation: {Error}", err);
            return;
        }

        TryLoadCompiledAssembly(outputPath);
    }

    private void CheckForScriptChanges()
    {
        var needsRecompile = false;

        foreach (var (scriptName, lastModified) in _scriptLastModified)
        {
            var scriptPath = Path.Combine(_scriptsDirectory, $"{scriptName}.cs");
            if (!File.Exists(scriptPath)) 
                continue;
            
            var currentModified = File.GetLastWriteTime(scriptPath);
            if (currentModified > lastModified)
            {
                needsRecompile = true;
                break;
            }
        }

        if (needsRecompile)
        {
            Logger.Information("Script changes detected, recompiling...");
            CompileAllScripts();
        }
    }

    private (bool Success, string[] Errors) CompileScript(string scriptName, string scriptContent)
    {
        _ = scriptName;
        _ = scriptContent;
        return EmitGameAssemblyToDisk();
    }

    private (bool Success, string[] Errors) EmitGameAssemblyToDisk()
    {
        var outputPath = AllocateGameAssemblyBuildPath();
        if (!GameAssemblyCompiler.TryCompile(
                _scriptsDirectory,
                outputPath,
                _debugMode,
                useDebugOptimization: _debugMode,
                out var errors))
            return (false, errors ?? [""]);

        TryLoadCompiledAssembly(outputPath);
        return (true, []);
    }

    private void TryLoadCompiledAssembly(string outputPath)
    {
        try
        {
            if (_debugMode && File.Exists(Path.ChangeExtension(outputPath, ".pdb")))
                _debugSymbols[GameAssemblyCompiler.AssemblyName] = File.ReadAllBytes(Path.ChangeExtension(outputPath, ".pdb")!);

            _dynamicAssembly = Assembly.LoadFrom(outputPath);
            IndexScriptSourcesFromDisk();
            UpdateScriptTypes();
            Logger.Information("Loaded {Count} script types from game assembly", _scriptTypes.Count);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load compiled game assembly");
        }
    }

    private void IndexScriptSourcesFromDisk()
    {
        _scriptSources.Clear();
        _scriptLastModified.Clear();
        if (!Directory.Exists(_scriptsDirectory))
            return;

        foreach (var scriptPath in Directory.GetFiles(_scriptsDirectory, "*.cs", SearchOption.AllDirectories))
        {
            var scriptName = Path.GetFileNameWithoutExtension(scriptPath);
            try
            {
                _scriptSources[scriptName] = File.ReadAllText(scriptPath, System.Text.Encoding.UTF8);
                _scriptLastModified[scriptName] = File.GetLastWriteTime(scriptPath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to index script file {Path}", scriptPath);
            }
        }
    }

    private static bool IsEditorProcess() =>
        AppDomain.CurrentDomain.GetAssemblies()
            .AsValueEnumerable()
            .Any(a => a.GetName().Name == "Editor");

    private string AllocateGameAssemblyBuildPath()
    {
        var stablePath = ResolveGameAssemblyOutputPath();
        if (IsEditorProcess())
        {
            var engineDir = Path.GetDirectoryName(stablePath);
            if (string.IsNullOrEmpty(engineDir))
                return stablePath;
            Directory.CreateDirectory(engineDir);
            return GameAssemblyCompiler.GetNextEditorBuildPath(engineDir);
        }

        return stablePath;
    }

    private string ResolveGameAssemblyOutputPath()
    {
        var fullScripts = Path.GetFullPath(_scriptsDirectory);
        var parentName = Path.GetFileName(fullScripts.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (!parentName.Equals("scripts", StringComparison.OrdinalIgnoreCase))
            return Path.Combine(fullScripts, "..", "..", "GameAssembly.dll");

        var assetsDir = Path.GetDirectoryName(fullScripts);
        if (assetsDir is null)
            return Path.Combine(fullScripts, "GameAssembly.dll");

        if (!Path.GetFileName(assetsDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
                .Equals("assets", StringComparison.OrdinalIgnoreCase))
            return Path.Combine(assetsDir, "GameAssembly.dll");

        var appRoot = Path.GetDirectoryName(assetsDir);
        if (appRoot is null)
            return Path.Combine(assetsDir, "GameAssembly.dll");

        if (IsEditorProcess())
            return Path.Combine(appRoot, ".engine", "GameAssembly.dll");

        return Path.Combine(appRoot, "GameAssembly.dll");
    }
    
    private void UpdateScriptTypes()
    {
        _scriptTypes.Clear();

        if (_dynamicAssembly == null) return;

        foreach (var type in _dynamicAssembly.GetTypes())
        {
            if (typeof(ScriptableEntity).IsAssignableFrom(type) && !type.IsAbstract)
            {
                _scriptTypes[type.Name] = type;
                Logger.Debug("Registered script type: {TypeName}", type.Name);
            }
        }
    }
    
    public void ForceRecompile()
    {
        Logger.Information("Force recompiling scripts for debugging...");
        CompileAllScripts();
        
        if (_sceneContext.ActiveScene == null) 
            return;

        var scriptEntities = _sceneContext.ActiveScene.Entities
            .AsValueEnumerable()
            .Where(e => e.HasComponent<NativeScriptComponent>());
        foreach (var entity in scriptEntities)
        {
            var scriptComponent = entity.GetComponent<NativeScriptComponent>();
            var scriptType = scriptComponent.ScriptableEntity?.GetType();
            if (scriptType == null || !_scriptTypes.ContainsKey(scriptType.Name)) 
                continue;
            
            var newInstance = CreateScriptInstance(scriptType.Name);
            if (newInstance.IsSuccess)
            {
                scriptComponent.ScriptableEntity = newInstance.Value;
                scriptComponent.ScriptableEntity.SetEntity(entity);
                scriptComponent.ScriptableEntity.SetSceneContext(_sceneContext);
                scriptComponent.ScriptableEntity.OnCreate();
            }
        }
    }
    
    public string GenerateScriptTemplate(string scriptName)
    {
        return $$"""
                 using System;
                 using System.Collections.Generic;
                 using System.Numerics;
                 using ECS;  // CRITICAL: For Entity class
                 using Engine.Scene;
                 using Engine.Core.Input;
                 using Engine.Scene.Components;

                 public class {{scriptName}} : ScriptableEntity
                 {
                     public override void OnCreate()
                     {
                         Console.WriteLine("{{scriptName}} created!");
                     }

                     public override void OnUpdate(TimeSpan ts)
                     {
                         // Your update logic here
                     }

                     public override void OnDestroy()
                     {
                         Console.WriteLine("{{scriptName}} destroyed!");
                     }
                     
                     public override void OnKeyPressed(KeyCodes key)
                     {
                         if (key == KeyCodes.Space)
                         {
                             Console.WriteLine("{{scriptName}} action triggered!");
                         }
                     }
                 }
                 """;
    }
}