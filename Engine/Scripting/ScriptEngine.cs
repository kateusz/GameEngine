using System.Reflection;
using CSharpFunctionalExtensions;
using ECS;
using Engine.Core.Input;
using Engine.Events;
using Engine.Scene;
using Engine.Scene.Components;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NLog;
using Entity = ECS.Entity;
using GameScene = Engine.Scene.Scene;

namespace Engine.Scripting;

public class ScriptEngine
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    public static ScriptEngine Instance { get; } = new();

    private readonly Dictionary<string, Type> _scriptTypes = new();
    private readonly Dictionary<string, DateTime> _scriptLastModified = new();
    private readonly Dictionary<string, string> _scriptSources = new();
    private readonly string _scriptsDirectory;
    private Assembly? _dynamicAssembly;

    private GameScene? _activeScene;

    private ScriptEngine()
    {
        // Create scripts directory if it doesn't exist
        _scriptsDirectory = Path.Combine(Environment.CurrentDirectory, "assets", "scripts");
        Directory.CreateDirectory(_scriptsDirectory);

        // Compile all existing scripts
        CompileAllScripts();
    }

    public void Initialize(GameScene scene)
    {
        _activeScene = scene;
        Logger.Info("ScriptEngine initialized with scene");
    }

    public void Update(TimeSpan deltaTime)
    {
        // Check for script changes
        CheckForScriptChanges();

        // Update all script components
        if (_activeScene == null) return;

        var scriptEntities = _activeScene.Entities.Where(e => e.HasComponent<NativeScriptComponent>());
        foreach (var entity in scriptEntities)
        {
            var scriptComponent = entity.GetComponent<NativeScriptComponent>();
            if (scriptComponent.ScriptableEntity == null) continue;

            // Initialize if needed
            if (scriptComponent.ScriptableEntity.Entity == null)
            {
                scriptComponent.ScriptableEntity.Entity = entity;
                try
                {
                    scriptComponent.ScriptableEntity.Init(_activeScene);
                    scriptComponent.ScriptableEntity.OnCreate();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Error initializing script on entity {entity.Name}");
                }
            }

            // Update script
            try
            {
                scriptComponent.ScriptableEntity.OnUpdate(deltaTime);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error updating script on entity {entity.Name}");
            }
        }
    }

    public void ProcessEvent(Event @event)
    {
        if (_activeScene == null) return;

        var scriptEntities = _activeScene.Entities.Where(e => e.HasComponent<NativeScriptComponent>());
        foreach (var entity in scriptEntities)
        {
            var scriptComponent = entity.GetComponent<NativeScriptComponent>();
            if (scriptComponent.ScriptableEntity == null) continue;

            // Forward events to appropriate script handlers
            try
            {
                switch (@event)
                {
                    case KeyPressedEvent kpe:
                        scriptComponent.ScriptableEntity.OnKeyPressed((KeyCodes)kpe.KeyCode);
                        break;
                    case MouseButtonPressedEvent mbpe:
                        scriptComponent.ScriptableEntity.OnMouseButtonPressed(mbpe.Button);
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error processing event in script on entity {entity.Name}");
            }
        }
    }

    public string[] GetAvailableScriptNames()
    {
        return _scriptTypes.Keys.ToArray();
    }

    public Type GetScriptType(string scriptName)
    {
        return _scriptTypes.TryGetValue(scriptName, out var type) ? type : null;
    }

    public string GetScriptSource(string scriptName)
    {
        if (_scriptSources.TryGetValue(scriptName, out var source))
        {
            return source;
        }

        var scriptPath = Path.Combine(_scriptsDirectory, $"{scriptName}.cs");
        if (File.Exists(scriptPath))
        {
            var src = File.ReadAllText(scriptPath);
            _scriptSources[scriptName] = src;
            return src;
        }

        return string.Empty;
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
            return instance is null ? Result.Failure<ScriptableEntity>($"Unable to create instance of {scriptType}") : Result.Success(instance);
        }
        catch (Exception ex)
        {
            var error = $"Failed to create instance of script '{scriptName}'";
            Logger.Error(ex, error);
            return Result.Failure<ScriptableEntity>(error);
        }
    }

    public async Task<(bool Success, string[] Errors)> CreateOrUpdateScriptAsync(string scriptName, string scriptContent)
    {
        var scriptPath = Path.Combine(_scriptsDirectory, $"{scriptName}.cs");
            
        try
        {
            // Save script content
            await File.WriteAllTextAsync(scriptPath, scriptContent);
            _scriptSources[scriptName] = scriptContent;
            _scriptLastModified[scriptName] = File.GetLastWriteTime(scriptPath);
                
            // Compile the script
            // todo: fix compile
            var (success, errors) = CompileScript(scriptName, scriptContent);
                
            if (success)
            {
                Logger.Info($"Script '{scriptName}' successfully compiled");
                return (true, []);
            }

            Logger.Error($"Failed to compile script '{scriptName}': {string.Join(", ", errors)}");
            return (false, errors);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Error saving or compiling script '{scriptName}'");
            return (false, [ex.Message]);
        }
    }

    public bool DeleteScript(string scriptName)
    {
        var scriptPath = Path.Combine(_scriptsDirectory, $"{scriptName}.cs");
            
        try
        {
            if (File.Exists(scriptPath))
            {
                File.Delete(scriptPath);
            }
                
            _scriptTypes.Remove(scriptName);
            _scriptSources.Remove(scriptName);
            _scriptLastModified.Remove(scriptName);
                
            // Recompile all scripts to ensure dependencies are handled
            CompileAllScripts();
                
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Error deleting script '{scriptName}'");
            return false;
        }
    }

    private void CheckForScriptChanges()
    {
        bool needsRecompile = false;
        var scriptFiles = Directory.GetFiles(_scriptsDirectory, "*.cs");
            
        // Check for modifications to existing scripts
        foreach (var script in _scriptLastModified.Keys.ToList())
        {
            var scriptPath = Path.Combine(_scriptsDirectory, $"{script}.cs");
            if (File.Exists(scriptPath))
            {
                var lastModified = File.GetLastWriteTime(scriptPath);
                if (lastModified > _scriptLastModified[script])
                {
                    _scriptLastModified[script] = lastModified;
                    _scriptSources[script] = File.ReadAllText(scriptPath);
                    needsRecompile = true;
                }
            }
        }
            
        // Check for new scripts
        foreach (var scriptPath in scriptFiles)
        {
            var scriptName = Path.GetFileNameWithoutExtension(scriptPath);
            if (!_scriptLastModified.ContainsKey(scriptName))
            {
                _scriptLastModified[scriptName] = File.GetLastWriteTime(scriptPath);
                _scriptSources[scriptName] = File.ReadAllText(scriptPath);
                needsRecompile = true;
            }
        }
            
        if (needsRecompile)
        {
            CompileAllScripts();
        }
    }

    private void CompileAllScripts()
    {
        Logger.Info("Compiling all scripts...");
            
        var scriptFiles = Directory.GetFiles(_scriptsDirectory, "*.cs");
        if (scriptFiles.Length == 0)
        {
            Logger.Info("No scripts found to compile");
            return;
        }
            
        var syntaxTrees = new List<SyntaxTree>();
            
        foreach (var scriptPath in scriptFiles)
        {
            var scriptName = Path.GetFileNameWithoutExtension(scriptPath);
            var scriptContent = File.ReadAllText(scriptPath);
                
            _scriptSources[scriptName] = scriptContent;
            _scriptLastModified[scriptName] = File.GetLastWriteTime(scriptPath);
                
            var syntaxTree = CSharpSyntaxTree.ParseText(scriptContent);
            syntaxTrees.Add(syntaxTree);
        }
            
        CompileScripts(syntaxTrees.ToArray());
    }

    private (bool Success, string[] Errors) CompileScript(string scriptName, string scriptContent)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(scriptContent);
            
        // Add existing scripts to compilation
        var syntaxTrees = new List<SyntaxTree> { syntaxTree };
        foreach (var (name, source) in _scriptSources)
        {
            if (name != scriptName)
            {
                syntaxTrees.Add(CSharpSyntaxTree.ParseText(source));
            }
        }
            
        return CompileScripts(syntaxTrees.ToArray());
    }

    private (bool Success, string[] Errors) CompileScripts(SyntaxTree[] syntaxTrees)
    {
        try
        {
            // Get required references
            var references = GetMetadataReferences();
                
            // Create compilation
            var compilationOptions = new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Debug,
                allowUnsafe: true);
                
            var compilation = CSharpCompilation.Create(
                "DynamicScripts",
                syntaxTrees,
                references,
                compilationOptions);
                
            // Emit to memory
            using var ms = new MemoryStream();
            var emitResult = compilation.Emit(ms);
                
            if (emitResult.Success)
            {
                ms.Seek(0, SeekOrigin.Begin);
                _dynamicAssembly = Assembly.Load(ms.ToArray());
                    
                // Update script types dictionary
                _scriptTypes.Clear();
                foreach (var type in _dynamicAssembly.GetTypes())
                {
                    if (typeof(ScriptableEntity).IsAssignableFrom(type) && !type.IsAbstract)
                    {
                        _scriptTypes[type.Name] = type;
                    }
                }
                    
                Logger.Info($"Successfully compiled {_scriptTypes.Count} scripts");
                return (true, []);
            }

            var errors = emitResult.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.GetMessage())
                .Distinct()
                .ToArray();
                    
            Logger.Error($"Script compilation failed with {errors.Length} errors");
            return (false, errors);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error during script compilation");
            return (false, [ex.Message]);
        }
    }

    private MetadataReference[] GetMetadataReferences()
    {
        var references = new List<MetadataReference>
        {
            // Add core assemblies
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Runtime.CompilerServices.RuntimeHelpers).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
            // Add engine assemblies
            MetadataReference.CreateFromFile(typeof(Entity).Assembly.Location), // ECS
            MetadataReference.CreateFromFile(typeof(Component).Assembly.Location), // ECS
            MetadataReference.CreateFromFile(typeof(GameScene).Assembly.Location), // Engine.Scene
            MetadataReference.CreateFromFile(typeof(ScriptableEntity).Assembly.Location), // Engine
            MetadataReference.CreateFromFile(typeof(TransformComponent).Assembly.Location), // Engine components
            MetadataReference.CreateFromFile(typeof(NativeScriptComponent).Assembly.Location), // Engine components
            MetadataReference.CreateFromFile(typeof(KeyCodes).Assembly.Location), // Engine.Core.Input
            MetadataReference.CreateFromFile(typeof(Event).Assembly.Location), // Engine.Events
            // Numerics for Vector3, etc.
            MetadataReference.CreateFromFile(typeof(System.Numerics.Vector3).Assembly.Location)
        };

        return references.Distinct().ToArray();
    }
        
    // Creates a default script template for a new script
    public static string GenerateScriptTemplate(string scriptName)
    {
        return $$"""
                 using System;
                 using System.Numerics;
                 using Engine.Scene;
                 using Engine.Core.Input;
                 using Engine.Scene.Components;

                 public class {{scriptName}} : ScriptableEntity
                 {
                     public override void Init(Scene currentScene)
                     {
                         base.Init(currentScene);
                         Console.WriteLine("{{scriptName}} initialized!");
                     }
                 
                     public override void OnCreate()
                     {
                         Console.WriteLine("{{scriptName}} created!");
                     }
                 
                     public override void OnUpdate(TimeSpan ts)
                     {
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