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
    IGameAssemblyBuilder builder,
    IScriptEngine scriptEngine,
    IComponentSerializerRegistry componentSerializerRegistry,
    Func<string, bool> ensureGameAssemblyRegistered)
{
    private static readonly ILogger Logger = Log.ForContext<GameScriptWorkspace>();

    private string? _appliedAssemblyKey;
    private readonly Dictionary<string, DateTime> _scriptLastModified = new();
    private readonly Dictionary<string, string> _scriptSources = new();
    private readonly Dictionary<string, byte[]> _debugSymbols = new();
    private string _scriptsDirectory = string.Empty;
    private string _outputDllPath = string.Empty;
    private bool _debugMode = true;

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

    public async Task<(bool Success, string[] Errors)> CreateOrUpdateScriptAsync(string scriptName, string scriptContent)
    {
        var scriptPath = Path.Combine(_scriptsDirectory, $"{scriptName}.cs");

        try
        {
            await File.WriteAllTextAsync(scriptPath, scriptContent);
            _scriptSources[scriptName] = scriptContent;
            _scriptLastModified[scriptName] = File.GetLastWriteTime(scriptPath);

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
            if (!_debugSymbols.TryGetValue(assemblyName, out var symbols))
                return false;

            File.WriteAllBytes($"{outputPath}.pdb", symbols);

            var assembly = scriptEngine.GetLoadedGameAssembly();
            if (assembly is not null && !string.IsNullOrEmpty(assembly.Location))
                File.Copy(assembly.Location, $"{outputPath}.dll", true);

            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to save debug symbols to {OutputPath}", outputPath);
            return false;
        }
    }

    public void PrintDebugInfo()
    {
        var assembly = scriptEngine.GetLoadedGameAssembly();
        var scriptCount = assembly?.GetTypes()
            .Count(t => typeof(ScriptableEntity).IsAssignableFrom(t) && !t.IsAbstract) ?? 0;

        Logger.Debug("=== SCRIPT WORKSPACE DEBUG INFO === DebugMode: {DebugMode}, ScriptsDirectory: {ScriptsDirectory}, Loaded Scripts: {ScriptCount}",
            _debugMode, _scriptsDirectory, scriptCount);

        if (assembly is not null)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (typeof(ScriptableEntity).IsAssignableFrom(type) && !type.IsAbstract)
                    Logger.Debug("  - {ScriptName}: {TypeFullName}", type.Name, type.FullName);
            }

            Logger.Debug("Assembly Location: {AssemblyLocation}", assembly.Location);
            Logger.Debug("Assembly Full Name: {AssemblyFullName}", assembly.FullName);
        }

        Logger.Debug("Debug Symbols Available: {DebugSymbolsAvailable}", _debugSymbols.Count > 0);
        Logger.Debug("===================================");
    }

    public void CompileAllScripts()
    {
        var (success, errors) = TryCompileAllScripts();
        if (success)
            return;

        foreach (var err in errors)
            Logger.Error("Script compilation: {Error}", err);
    }

    public (bool Success, string[] Errors) TryCompileAllScripts()
    {
        if (string.IsNullOrEmpty(_scriptsDirectory) || string.IsNullOrEmpty(_outputDllPath))
            return (false, ["Scripts directory not configured"]);

        Logger.Information("Compiling all scripts to {GameAssembly}...", GameAssemblyCompiler.AssemblyName);
        if (!Directory.Exists(_scriptsDirectory))
        {
            var error = $"Scripts directory does not exist: {_scriptsDirectory}";
            Logger.Warning("{Error}", error);
            return (false, [error]);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(_outputDllPath)!);
        if (!builder.TryBuild(_scriptsDirectory, _outputDllPath, _debugMode, out var errors))
            return (false, errors);

        if (_debugMode && File.Exists(Path.ChangeExtension(_outputDllPath, ".pdb")))
            _debugSymbols[GameAssemblyCompiler.AssemblyName] = File.ReadAllBytes(Path.ChangeExtension(_outputDllPath, ".pdb")!);

        IndexScriptSourcesFromDisk();
        LoadGameAssemblyFromFile(_outputDllPath, _scriptsDirectory);

        return scriptEngine.GetLoadedGameAssembly() is null
            ? (false, ["Failed to load compiled game assembly"])
            : (true, []);
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
        if (GetLoadedGameAssembly() is null)
            TryCompileAllScripts();
        else if (GetLoadedGameAssembly() is { } assembly)
            ApplyLoadedAssembly(assembly);
    }

    public void LoadGameAssemblyFromFile(string dllPath, string scriptsDirectory)
    {
        scriptEngine.LoadGameAssemblyFromFile(dllPath, scriptsDirectory);
        if (scriptEngine.GetLoadedGameAssembly() is { } assembly)
            ApplyLoadedAssembly(assembly);
    }

    public void ApplyLoadedAssembly(Assembly assembly)
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
            var registrationKey = string.IsNullOrWhiteSpace(assembly.Location)
                ? key
                : Path.GetFullPath(assembly.Location);

            if (!ensureGameAssemblyRegistered(registrationKey))
                Logger.Debug("Game assembly at {Key} has no types marked with [Register]", registrationKey);

            componentSerializerRegistry.RegisterFromAssembly(assembly);
            _appliedAssemblyKey = key;
            Logger.Information("Applied loaded game assembly: {Key}", key);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to apply loaded game assembly: {Key}", key);
        }
    }

    public void ForceRecompile(IContext context, ScriptRuntimeStore store)
    {
        Logger.Information("Force recompiling scripts...");
        CompileAllScripts();
        NativeScriptIteration.Refresh(context, scriptEngine, store);
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

    public string GenerateScriptTemplate(string scriptName) =>
        $$"""
          using Audio;
          using ECS;
          using Input;
          using Math;
          using SceneComponents;
          using SceneComponents.Camera;
          using Scripting;

          public class {{scriptName}} : ScriptableEntity
          {
             public {{scriptName}}(IComponentAccessor componentAccessor, IAudio audio, IAudioPlayback audioPlayback) : base(componentAccessor, audio, audioPlayback) {}
          
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

    private void IndexScriptSourcesFromDisk()
    {
        _scriptSources.Clear();
        _scriptLastModified.Clear();
        if (!Directory.Exists(_scriptsDirectory))
            return;

        foreach (var scriptPath in GameAssemblyCompiler.EnumerateGameScriptFiles(_scriptsDirectory))
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
}
