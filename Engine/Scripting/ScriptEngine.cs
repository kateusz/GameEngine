using System.Reflection;
using CSharpFunctionalExtensions;
using Engine.Core.Input;
using Engine.Events;
using Engine.Events.Input;
using Engine.Scene;
using Engine.Scene.Components;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Serilog;
using ZLinq;

namespace Engine.Scripting;

public sealed class ScriptEngine : IScriptEngine
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
        CompileAllScripts();
    }

    public void OnUpdate(TimeSpan deltaTime)
    {
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
                scriptComponent.ScriptableEntity.Entity = entity;
                scriptComponent.ScriptableEntity.SceneContext = _sceneContext;
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

        var errors = new List<Exception>();

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
                    Logger.Error(ex, $"Error in script OnDestroy for entity '{entity.Name}' (ID: {entity.Id})");
                    errors.Add(ex);
                }
            }
        }

        if (errors.Count > 0)
        {
            Logger.Warning(
                "Scene stopped with {ErrorsCount} script error(s) during OnDestroy. Check logs above for details.",
                errors.Count);
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
                        scriptComponent.ScriptableEntity.OnKeyPressed((KeyCodes)kpe.KeyCode);
                        break;
                    case KeyReleasedEvent kpe:
                        scriptComponent.ScriptableEntity.OnKeyReleased((KeyCodes)kpe.KeyCode);
                        break;
                    case MouseButtonPressedEvent mbpe:
                        scriptComponent.ScriptableEntity.OnMouseButtonPressed(mbpe.Button);
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
    
    public bool SaveDebugSymbols(string outputPath, string assemblyName = "DynamicScripts")
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
        Logger.Debug("=== SCRIPT ENGINE DEBUG INFO ===");
        Logger.Debug("Debug Mode: {DebugMode}", _debugMode);
        Logger.Debug("Scripts Directory: {ScriptsDirectory}", _scriptsDirectory);
        Logger.Debug("Loaded Scripts: {ScriptCount}", _scriptTypes.Count);

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
        Logger.Information("Compiling all scripts...");

        var scriptFiles = Directory.GetFiles(_scriptsDirectory, "*.cs");
        if (scriptFiles.Length == 0)
        {
            Logger.Information("No scripts found to compile");
            return;
        }

        var syntaxTrees = new List<SyntaxTree>();

        foreach (var scriptPath in scriptFiles)
        {
            var scriptName = Path.GetFileNameWithoutExtension(scriptPath);

            try
            {
                var scriptContent = File.ReadAllText(scriptPath, System.Text.Encoding.UTF8);

                _scriptSources[scriptName] = scriptContent;
                _scriptLastModified[scriptName] = File.GetLastWriteTime(scriptPath);
                
                var syntaxTree = CSharpSyntaxTree.ParseText(
                    text: scriptContent,
                    options: CSharpParseOptions.Default,
                    path: scriptPath,
                    encoding: System.Text.Encoding.UTF8);

                syntaxTrees.Add(syntaxTree);
                Logger.Debug("✅ Loaded script: {ScriptName} with encoding: UTF-8", scriptName);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to load script: {ScriptName}", scriptName);
            }
        }

        if (syntaxTrees.Count > 0)
        {
            CompileScripts(syntaxTrees.ToArray());
        }
        else
        {
            Logger.Warning("No scripts successfully loaded for compilation");
        }
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
        var scriptPath = Path.Combine(_scriptsDirectory, $"{scriptName}.cs");

        try
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(
                text: scriptContent,
                options: CSharpParseOptions.Default,
                path: scriptPath,
                encoding: System.Text.Encoding.UTF8);

            var syntaxTrees = new List<SyntaxTree> { syntaxTree };

            // TODO: check performance
            foreach (var (name, source) in _scriptSources)
            {
                if (name == scriptName) 
                    continue;
                
                var existingPath = Path.Combine(_scriptsDirectory, $"{name}.cs");
                
                var existingContent = source;
                if (File.Exists(existingPath))
                {
                    existingContent = File.ReadAllText(existingPath, System.Text.Encoding.UTF8);
                }

                var existingTree = CSharpSyntaxTree.ParseText(
                    text: existingContent,
                    options: CSharpParseOptions.Default,
                    path: existingPath,
                    encoding: System.Text.Encoding.UTF8);

                syntaxTrees.Add(existingTree);
            }

            return CompileScripts(syntaxTrees.ToArray());
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error preparing compilation for script: {ScriptName}", scriptName);
            return (false, [ex.Message]);
        }
    }

    private (bool Success, string[] Errors) CompileScripts(SyntaxTree[] syntaxTrees)
    {
        try
        {
            var references = GetReferencesFromRuntimeDirectory();
            
            var validationResult = ValidateReferences(references);
            if (!validationResult.Success)
            {
                return (false, validationResult.Errors);
            }
            
            var compilationOptions = new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: _debugMode ? OptimizationLevel.Debug : OptimizationLevel.Release,
                allowUnsafe: true,
                platform: Microsoft.CodeAnalysis.Platform.AnyCpu,
                warningLevel: 4,
                deterministic: true,
                checkOverflow: false);

            var compilation = CSharpCompilation.Create(
                "DynamicScripts",
                syntaxTrees,
                references,
                compilationOptions);
            
            var preEmitDiagnostics = compilation.GetDiagnostics();
            Logger.Debug("=== PRE-EMIT DIAGNOSTICS ({DiagnosticsCount}) ===", preEmitDiagnostics.Length);

            var errorDiagnostics = preEmitDiagnostics
                .AsValueEnumerable()
                .Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();

            if (errorDiagnostics.Length > 0)
            {
                Logger.Error("❌ COMPILATION ERRORS DETECTED:");
                foreach (var diagnostic in errorDiagnostics)
                {
                    Logger.Error("ERROR: {Message}", diagnostic.GetMessage());
                    Logger.Error("  Location: {Location}", diagnostic.Location);
                    Logger.Error("  Id: {DiagnosticId}", diagnostic.Id);
                }

                var errors = errorDiagnostics.Select(d => d.GetMessage()).ToArray();
                return (false, errors);
            }

            foreach (var diagnostic in preEmitDiagnostics)
            {
                Logger.Debug("{Severity}: {Message}", diagnostic.Severity, diagnostic.GetMessage());
                if (diagnostic.Location != Location.None)
                {
                    Logger.Debug("  Location: {Location}", diagnostic.Location);
                }

                Logger.Debug("  Id: {DiagnosticId}", diagnostic.Id);
                Logger.Debug("");
            }
            
            var emitOptions = new EmitOptions(
                debugInformationFormat: _debugMode
                    ? DebugInformationFormat.PortablePdb
                    : DebugInformationFormat.Embedded,
                includePrivateMembers: _debugMode);
            
            using var assemblyStream = new MemoryStream();
            using var symbolsStream = _debugMode ? new MemoryStream() : null;

            var emitResult = compilation.Emit(
                peStream: assemblyStream,
                pdbStream: symbolsStream,
                options: emitOptions);

            Logger.Debug("=== EMIT RESULT: {EmitSuccess} ===", emitResult.Success);

            if (!emitResult.Success)
            {
                Logger.Error("=== EMIT DIAGNOSTICS ===");
                foreach (var diagnostic in emitResult.Diagnostics)
                {
                    Logger.Error("{Severity}: {Message}", diagnostic.Severity, diagnostic.GetMessage());
                }

                var errors = emitResult.Diagnostics
                    .AsValueEnumerable()
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .Select(d => d.GetMessage())
                    .Distinct()
                    .ToArray();

                Logger.Error("Script compilation failed with {ErrorCount} errors", errors.Length);
                return (false, errors);
            }

            // Load assembly with debug symbols
            assemblyStream.Seek(0, SeekOrigin.Begin);
            var assemblyBytes = assemblyStream.ToArray();

            byte[]? symbolBytes = null;
            if (symbolsStream != null)
            {
                symbolsStream.Seek(0, SeekOrigin.Begin);
                symbolBytes = symbolsStream.ToArray();

                // Store debug symbols for later use
                _debugSymbols["DynamicScripts"] = symbolBytes;
            }

            // Load assembly with debug information
            _dynamicAssembly = symbolBytes != null
                ? Assembly.Load(assemblyBytes, symbolBytes)
                : Assembly.Load(assemblyBytes);
            
            UpdateScriptTypes();

            Logger.Information("Successfully compiled {ScriptCount} scripts with debug support: {DebugMode}",
                _scriptTypes.Count, _debugMode);
            return (true, []);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error during script compilation");
            return (false, [ex.Message]);
        }
    }

    private static (bool Success, string[] Errors) ValidateReferences(MetadataReference[] references)
    {
        var errors = new List<string>();
        var referenceNames = new HashSet<string>();

        // Extract assembly names from references
        foreach (var reference in references)
        {
            if (reference is PortableExecutableReference peRef && !string.IsNullOrEmpty(peRef.FilePath))
            {
                var fileName = Path.GetFileNameWithoutExtension(peRef.FilePath);
                referenceNames.Add(fileName);
            }
        }

        Logger.Debug("=== REFERENCE VALIDATION ({ReferenceCount} references) ===", referenceNames.Count);
        var requiredAssemblies = new[]
        {
            "System.Private.CoreLib",
            "System.Runtime",
            "System.Numerics.Vectors",
            "ECS"
        };

        foreach (var required in requiredAssemblies)
        {
            if (referenceNames.Contains(required))
            {
                Logger.Debug("✅ Required assembly found: {AssemblyName}", required);
            }
            else
            {
                var error = $"❌ MISSING REQUIRED ASSEMBLY: {required}";
                Logger.Error(error);
                errors.Add($"Missing required assembly: {required}");
            }
        }

        // Check for engine assemblies
        var engineAssemblies = new[] { "Engine", "ECS", "Editor" };

        var foundEngineAssemblies = referenceNames
            .AsValueEnumerable()
            .Where(name => engineAssemblies.Any(name.StartsWith)).ToArray();

        Logger.Debug("Engine assemblies found: {EngineAssemblies}", string.Join(", ", foundEngineAssemblies));

        if (!foundEngineAssemblies.Any(name => name == "ECS"))
        {
            errors.Add("ECS assembly is required but not found. Scripts cannot access Entity class without it.");
        }

        Logger.Debug("=== VALIDATION RESULT: {ValidationResult} ===", errors.Count == 0 ? "SUCCESS" : "FAILED");

        return (errors.Count == 0, errors.ToArray());
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

    private MetadataReference[] GetReferencesFromRuntimeDirectory()
    {
        Logger.Debug("=== LOADING REFERENCES FOR SCRIPT COMPILATION ===");
        var references = new List<MetadataReference>();

        try
        {
            var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location);
            Logger.Debug("Runtime directory: {RuntimeDir}", runtimeDir);

            // Essential .NET 8 assemblies
            var essentialAssemblies = new[]
            {
                "System.Private.CoreLib.dll",
                "System.Runtime.dll",
                "System.Collections.dll",
                "System.Console.dll",
                "System.Linq.dll",
                "System.Numerics.dll",
                "System.Numerics.Vectors.dll",
                "netstandard.dll",
                "mscorlib.dll",
                "System.Collections.Concurrent.dll"
            };

            foreach (var assemblyName in essentialAssemblies)
            {
                var path = Path.Combine(runtimeDir, assemblyName);
                if (File.Exists(path))
                {
                    try
                    {
                        references.Add(MetadataReference.CreateFromFile(path));
                        Logger.Debug("✅ Added .NET: {AssemblyName}", assemblyName);
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning(ex, "❌ Error adding {AssemblyName}", assemblyName);
                    }
                }
                else
                {
                    Logger.Warning("❌ Missing: {AssemblyName}", assemblyName);
                }
            }
            
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .AsValueEnumerable()
                .Where(a => !a.IsDynamic)
                .ToArray();

            Logger.Debug("Found {AssemblyCount} loaded assemblies", loadedAssemblies.Length);

            foreach (var assembly in loadedAssemblies)
            {
                var name = assembly.GetName().Name;
                Logger.Debug("Checking assembly: {AssemblyName}", name);

                // Check for engine-related assemblies
                if (!name!.StartsWith("Engine") && !name.StartsWith("ECS") && !name.StartsWith("Editor")) 
                    continue;
                
                try
                {
                    // Handle assemblies without location (in-memory assemblies)
                    if (!string.IsNullOrEmpty(assembly.Location))
                    {
                        references.Add(MetadataReference.CreateFromFile(assembly.Location));
                        Logger.Debug("✅ Added engine assembly: {AssemblyName} from {Location}", name,
                            assembly.Location);
                    }
                    else
                    {
                        // For in-memory assemblies, we need to find them in the output directory
                        var currentDir = Environment.CurrentDirectory;
                        var possiblePaths = new[]
                        {
                            Path.Combine(currentDir, $"{name}.dll"),
                            Path.Combine(currentDir, "bin", "Debug", "net8.0", $"{name}.dll"),
                            Path.Combine(currentDir, "..", name, "bin", "Debug", "net8.0", $"{name}.dll")
                        };

                        var found = false;
                        foreach (var possiblePath in possiblePaths)
                        {
                            if (File.Exists(possiblePath))
                            {
                                references.Add(MetadataReference.CreateFromFile(possiblePath));
                                Logger.Debug("✅ Added engine assembly: {AssemblyName} from {Path}", name,
                                    possiblePath);
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            Logger.Warning("❌ Could not find assembly file for: {AssemblyName}", name);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning(ex, "❌ Error adding engine assembly {AssemblyName}", name);
                }
            }
            
            var ecsAssemblyPath = FindECSAssembly();
            if (!string.IsNullOrEmpty(ecsAssemblyPath))
            {
                try
                {
                    references.Add(MetadataReference.CreateFromFile(ecsAssemblyPath));
                    Logger.Debug("✅ Added ECS assembly: {ECSAssemblyPath}", ecsAssemblyPath);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "❌ Error adding ECS assembly");
                }
            }
            else
            {
                Logger.Error("❌ CRITICAL: ECS assembly not found! Scripts will fail to compile.");
            }
            
            AddBox2D(references);

            Logger.Debug("Total references added: {ReferenceCount}", references.Count);
            return references.ToArray();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "❌ Error loading references");
            throw;
        }
    }

    private static void AddBox2D(List<MetadataReference> references)
    {
        try
        {
            var box2dPath = Path.Combine(Environment.CurrentDirectory, "Box2D.NetStandard.dll");
            if (File.Exists(box2dPath))
            {
                references.Add(MetadataReference.CreateFromFile(box2dPath));
                Logger.Debug("✅ Added Box2D: {Box2DPath}", box2dPath);
            }
            else
            {
                Logger.Debug("❌ Box2D not found at: {Box2DPath}", box2dPath);
            }
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "❌ Error adding Box2D");
        }
    }

    private string? FindECSAssembly()
    {
        // Try to find ECS assembly in various locations
        var currentDir = Environment.CurrentDirectory;
        var possiblePaths = new[]
        {
            Path.Combine(currentDir, "ECS.dll"),
            Path.Combine(currentDir, "bin", "Debug", "net8.0", "ECS.dll"),
            Path.Combine(currentDir, "..", "ECS", "bin", "Debug", "net8.0", "ECS.dll"),
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ECS.dll")
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                Logger.Debug("Found ECS assembly at: {Path}", path);
                return path;
            }
        }
        
        var ecsAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .AsValueEnumerable()
            .FirstOrDefault(a => a.GetName().Name == "ECS");

        if (ecsAssembly != null && !string.IsNullOrEmpty(ecsAssembly.Location))
        {
            Logger.Debug("Found ECS assembly from loaded assemblies: {Location}", ecsAssembly.Location);
            return ecsAssembly.Location;
        }

        Logger.Warning("ECS assembly not found in any location");
        return null;
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
                scriptComponent.ScriptableEntity.Entity = entity;
                scriptComponent.ScriptableEntity.SceneContext = _sceneContext;
                scriptComponent.ScriptableEntity.OnCreate();
            }
        }
    }
    
    public static string GenerateScriptTemplate(string scriptName)
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