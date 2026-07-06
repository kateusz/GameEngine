using System.Reflection;
using ECS;
using Engine.Scene;
using Engine.Scene.Serializer;
using Engine.Scene.Systems;
using Engine.Scripting;
using Scripting;
using Serilog;

namespace Editor.Features.Scripting;

public sealed class GameScriptWorkspace(
    IScriptEngine scriptEngine,
    IComponentSerializerRegistry componentSerializerRegistry,
    Func<Assembly, bool> ensureGameAssemblyRegistered,
    Action<Assembly> revokeGameAssemblyRegistrations)
{
    private static readonly ILogger Logger = Log.ForContext<GameScriptWorkspace>();

    private Assembly? _appliedAssembly;
    private string? _appliedAssemblyKey;
    private readonly Dictionary<string, string> _scriptSources = new();
    private string _scriptsDirectory = string.Empty;
    private string _outputDllPath = string.Empty;
    private const bool DebugMode = true;

    public static string ResolveEditorDllPath(string projectDir) =>
        Path.Combine(projectDir, ".engine", "GameAssembly.dll");

    public void SetScriptsDirectory(string scriptsDirectory, string outputDllPath)
    {
        _scriptsDirectory = scriptsDirectory;
        _outputDllPath = Path.GetFullPath(outputDllPath);
        Directory.CreateDirectory(_scriptsDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(_outputDllPath)!);
        CompileAllScripts();
    }

    public async Task<(bool Success, string[] Errors)> CreateOrUpdateScriptAsync(string scriptName,
        string scriptContent)
    {
        var scriptPath = Path.Combine(_scriptsDirectory, $"{scriptName}.cs");

        try
        {
            await File.WriteAllTextAsync(scriptPath, scriptContent);
            _scriptSources[scriptName] = scriptContent;

            var (success, errors) = TryCompileAllScripts();
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

            _scriptSources.Remove(scriptName);
            CompileAllScripts();
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error deleting script '{ScriptName}'", scriptName);
            return false;
        }
    }

    public (bool Success, string[] Errors) TryCompileAllScripts()
    {
        if (string.IsNullOrEmpty(_scriptsDirectory) || string.IsNullOrEmpty(_outputDllPath))
            return (false, ["Scripts directory not configured"]);

        if (!Directory.Exists(_scriptsDirectory))
        {
            var error = $"Scripts directory does not exist: {_scriptsDirectory}";
            Logger.Warning("{Error}", error);
            return (false, [error]);
        }

        return ReloadGameAssembly(compile: true, dllPath: _outputDllPath);
    }

    public Type? GetLoadedGameType(string typeName)
    {
        var assembly = scriptEngine.GetLoadedGameAssembly();
        if (assembly is null || string.IsNullOrWhiteSpace(typeName))
            return null;

        return Array.Find(assembly.GetTypes(), t =>
            t is { IsClass: true, IsAbstract: false }
            && t.Name == typeName
            && typeof(IGameComponent).IsAssignableFrom(t));
    }

    public Assembly? GetLoadedGameAssembly() => scriptEngine.GetLoadedGameAssembly();

    public void EnsureScriptsCompiledAndApplied()
    {
        if (GetLoadedGameAssembly() is { } assembly && IsCurrentProjectAssembly(assembly))
            ApplyLoadedAssembly(assembly);
        else
            TryCompileAllScripts();
    }

    public void RestoreEditAssembly()
    {
        if (string.IsNullOrEmpty(_scriptsDirectory))
            return;

        TryCompileAllScripts();
    }

    public void LoadGameAssemblyFromFile(string dllPath, string scriptsDirectory)
    {
        _scriptsDirectory = scriptsDirectory;
        ReloadGameAssembly(compile: false, dllPath: Path.GetFullPath(dllPath));
    }

    public void RevokeAndUnload()
    {
        RevokeAppliedAssembly();
        scriptEngine.UnloadGameAssembly();
    }

    public void ForceRecompile(IContext context, ScriptRuntimeStore store)
    {
        Logger.Information("Force recompiling scripts...");
        ReloadGameAssembly(compile: true, dllPath: _outputDllPath, context: context, store: store);
    }

    public string[] GetAvailableScriptNames()
    {
        var assembly = scriptEngine.GetLoadedGameAssembly();
        if (assembly is null)
            return [];

        return assembly.GetTypes()
            .Where(t => typeof(ScriptableEntity).IsAssignableFrom(t) && !t.IsAbstract)
            .Select(t => t.Name)
            .ToArray();
    }

    public string? GetScriptFilePath(string scriptName)
    {
        var scriptPath = Path.Combine(_scriptsDirectory, $"{scriptName}.cs");
        return File.Exists(scriptPath) ? scriptPath : null;
    }

    private void CompileAllScripts()
    {
        var (success, errors) = TryCompileAllScripts();
        if (success)
            return;

        foreach (var err in errors)
            Logger.Error("Script compilation: {Error}", err);
    }
    
    private void RevokeAppliedAssembly()
    {
        if (_appliedAssembly is null)
            return;

        componentSerializerRegistry.UnregisterAssembly(_appliedAssembly);
        revokeGameAssemblyRegistrations(_appliedAssembly);
        _appliedAssembly = null;
        _appliedAssemblyKey = null;
    }
    
    private void ApplyLoadedAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var key = string.IsNullOrWhiteSpace(assembly.Location)
            ? assembly.FullName ?? assembly.GetName().Name ?? string.Empty
            : Path.GetFullPath(assembly.Location);

        if (string.IsNullOrWhiteSpace(key))
            return;

        if (_appliedAssemblyKey is not null
            && string.Equals(_appliedAssemblyKey, key, StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            if (!ensureGameAssemblyRegistered(assembly))
                Logger.Debug("Game assembly at {Key} has no types marked with [Register]", key);

            componentSerializerRegistry.RegisterFromAssembly(assembly);
            _appliedAssembly = assembly;
            _appliedAssemblyKey = key;
            Logger.Information("Applied loaded game assembly: {Key}", key);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to apply loaded game assembly: {Key}", key);
        }
    }

    private (bool Success, string[] Errors) ReloadGameAssembly(
        bool compile,
        string dllPath,
        IContext? context = null,
        ScriptRuntimeStore? store = null)
    {
        if (context is not null && store is not null)
        {
            NativeScriptIteration.Shutdown(context, store);
            store.Clear();
        }

        RevokeAppliedAssembly();
        scriptEngine.UnloadGameAssembly();

        if (compile)
        {
            Logger.Information("Compiling all scripts to {GameAssembly}...", GameAssemblyCompiler.AssemblyName);
            var engineDir = Path.GetDirectoryName(dllPath)!;
            Directory.CreateDirectory(engineDir);
            dllPath = GameAssemblyCompiler.GetNextEditorBuildPath(engineDir);
            if (!GameAssemblyCompiler.TryCompile(_scriptsDirectory, dllPath, DebugMode, DebugMode, out var errors))
                return (false, errors);

            IndexScriptSourcesFromDisk();
        }

        scriptEngine.LoadGameAssemblyFromFile(dllPath);
        if (scriptEngine.GetLoadedGameAssembly() is not { } assembly)
            return (false, ["Failed to load compiled game assembly"]);

        ApplyLoadedAssembly(assembly);

        if (context is not null && store is not null)
            NativeScriptIteration.Refresh(context, scriptEngine, store);

        return (true, []);
    }

    private bool IsCurrentProjectAssembly(Assembly assembly)
    {
        if (string.IsNullOrEmpty(assembly.Location) || string.IsNullOrEmpty(_outputDllPath))
            return false;

        var engineDir = Path.GetDirectoryName(_outputDllPath);
        if (string.IsNullOrEmpty(engineDir))
            return false;

        var loadedDir = Path.GetDirectoryName(Path.GetFullPath(assembly.Location));
        return string.Equals(loadedDir, Path.GetFullPath(engineDir), StringComparison.OrdinalIgnoreCase);
    }

    private void IndexScriptSourcesFromDisk()
    {
        _scriptSources.Clear();
        if (!Directory.Exists(_scriptsDirectory))
            return;

        foreach (var scriptPath in GameAssemblyCompiler.EnumerateGameScriptFiles(_scriptsDirectory))
        {
            var scriptName = Path.GetFileNameWithoutExtension(scriptPath);
            try
            {
                _scriptSources[scriptName] = File.ReadAllText(scriptPath, System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to index script file {Path}", scriptPath);
            }
        }
    }
}