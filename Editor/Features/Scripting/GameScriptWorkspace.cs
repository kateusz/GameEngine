using System.Reflection;
using ECS;
using Editor.Scripting;
using Engine.Scene.Serializer;
using Engine.Scripting;
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

    private (bool Success, string[] Errors) ReloadGameAssembly(bool compile, string dllPath)
    {
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
        }

        scriptEngine.LoadGameAssemblyFromFile(dllPath);
        if (scriptEngine.GetLoadedGameAssembly() is not { } assembly)
            return (false, ["Failed to load compiled game assembly"]);

        ApplyLoadedAssembly(assembly);
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
}
