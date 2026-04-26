using System.Text.Json;
using ECS;
using ECS.Systems;
using Editor.Features.Project;
using Editor.Publisher;
using Engine.Scene;
using Engine.Scene.Serializer;
using Engine.Scripting;
using Serilog;

namespace Editor.Features.Scene;

public class SceneManager(
    ISceneContext sceneContext,
    ISceneSerializer sceneSerializer,
    SceneFactory sceneFactory,
    IGameAssemblySystemsBridge gameAssemblySystemsBridge,
    IProjectManager projectManager,
    ISystemManager systemManager,
    IGameAssemblyBuilder gameAssemblyBuilder,
    IScriptEngine scriptEngine)
    : ISceneManager
{
    private static readonly ILogger Logger = Log.ForContext<SceneManager>();

    public string? EditorScenePath { get; private set; }
    private bool _gameAssemblyRegistered;
    private string? _registeredAssemblyName;

    public void New(string sceneName)
    {
        sceneContext.ActiveScene?.Dispose();

        sceneContext.SetScene(sceneFactory.Create(path: "", sceneName));
        Logger.Information("📄 New scene created");
    }

    public void Open(string path)
    {
        if (sceneContext.State != SceneState.Edit)
            Stop();

        sceneContext.ActiveScene?.Dispose();
        EditorScenePath = null;
        
        EditorScenePath = path;
        sceneContext.SetScene(sceneFactory.Create(path, Path.GetFileNameWithoutExtension(path)));
        sceneSerializer.Deserialize(sceneContext.ActiveScene!, path);
        Logger.Information("📂 Scene opened: {Path}", path);
    }

    public void Save()
    {
        var sceneDir = PathBuilder.Build("scenes");
        if (!Directory.Exists(sceneDir))
            Directory.CreateDirectory(sceneDir);

        EditorScenePath = Path.Combine(sceneDir, $"{sceneContext.ActiveScene.Name}.scene");
        sceneSerializer.Serialize(sceneContext.ActiveScene, EditorScenePath);
        Logger.Information("💾 Scene saved: {EditorScenePath}", EditorScenePath);
    }

    public void Play()
    {
        if (string.IsNullOrEmpty(projectManager.CurrentProjectDirectory) || projectManager.ScriptsDir is null)
        {
            Logger.Warning("No project or scripts directory — open a project before Play.");
            return;
        }

        var engineDir = Path.Combine(projectManager.CurrentProjectDirectory, ".engine");
        Directory.CreateDirectory(engineDir);
        var dllPath = GameAssemblyCompiler.GetNextEditorBuildPath(engineDir);
        if (!gameAssemblyBuilder.TryBuild(projectManager.ScriptsDir, dllPath, emitPdb: true, out var buildErrors))
        {
            foreach (var e in buildErrors)
                Logger.Error("Game script build: {Error}", e);
            return;
        }

        scriptEngine.LoadGameAssemblyFromFile(dllPath, projectManager.ScriptsDir);
        scriptEngine.SetSuppressFileChangeRecompile(true);

        EnsureGameAssemblyRegistered(dllPath);
        RegisterGameSystems();
        sceneContext.SetState(SceneState.Play);
        sceneContext.ActiveScene.OnRuntimeStart();
        Logger.Information("▶️ Scene play started");
    }

    public void Stop()
    {
        sceneContext.SetState(SceneState.Edit);
        sceneContext.ActiveScene.OnRuntimeStop();
        scriptEngine.SetSuppressFileChangeRecompile(false);
        if (!string.IsNullOrEmpty(projectManager.ScriptsDir))
            scriptEngine.SetScriptsDirectory(projectManager.ScriptsDir);

        if (!string.IsNullOrEmpty(EditorScenePath))
        {
            Open(EditorScenePath);
        }

        Logger.Information("⏹️ Scene play stopped");
    }

    public void Restart()
    {
        if (string.IsNullOrEmpty(EditorScenePath))
        {
            Logger.Warning("Cannot restart scene: no scene path set (scene not saved)");
            return;
        }

        Stop();
        Open(EditorScenePath);
        Play();
        Logger.Information("🔄 Scene restarted");
    }

    public void DuplicateEntity(Entity entity)
    {
        if (sceneContext.State != SceneState.Edit)
            return;

        sceneContext.ActiveScene?.DuplicateEntity(entity);
        Logger.Information("📋 Entity duplicated: {EntityName}", entity.Name);
    }

    public string? GetCurrentScenePath() => EditorScenePath;

    private void EnsureGameAssemblyRegistered(string? builtDllPath = null)
    {
        var key = builtDllPath ?? ResolveGameAssemblyName();
        if (string.IsNullOrWhiteSpace(key))
            return;

        if (_gameAssemblyRegistered && string.Equals(_registeredAssemblyName, key, StringComparison.Ordinal))
            return;

        try
        {
            if (!gameAssemblySystemsBridge.EnsureRegistered(key))
            {
                Logger.Warning("Game assembly at {Key} does not expose static IoCContainer.Register", key);
                return;
            }
            
            _gameAssemblyRegistered = true;
            _registeredAssemblyName = key;
            Logger.Information("Registered game assembly: {Key}", key);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to register game assembly: {Key}", key);
        }
    }

    private void RegisterGameSystems()
    {
        IReadOnlyList<IGameSystem> gameSystems;
        try
        {
            gameSystems = gameAssemblySystemsBridge.ResolveSystems();
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to resolve game systems from container");
            return;
        }

        foreach (var gameSystem in gameSystems)
        {
            try
            {
                systemManager.RegisterSystem(gameSystem);
            }
            catch (InvalidOperationException)
            {
                // Scene can re-enter play with already registered singleton instances.
            }
        }
    }

    private string ResolveGameAssemblyName()
    {
        try
        {
            var projectDir = projectManager.CurrentProjectDirectory;
            if (string.IsNullOrWhiteSpace(projectDir))
                return string.Empty;

            if (!string.IsNullOrEmpty(projectManager.ScriptsDir))
            {
                var engineDir = Path.Combine(projectDir, ".engine");
                if (Directory.Exists(engineDir))
                {
                    var match = Directory.GetFiles(engineDir, "GameAssembly*.dll", SearchOption.TopDirectoryOnly)
                        .Select(p => new FileInfo(p))
                        .OrderByDescending(f => f.LastWriteTimeUtc)
                        .FirstOrDefault();
                    if (match is not null)
                        return match.FullName;
                }
            }

            var configPath = Path.Combine(projectDir, "game.config.json");
            if (!File.Exists(configPath))
                return string.Empty;

            var json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<GameConfiguration>(json);
            if (config is null)
                return string.Empty;
            if (!string.IsNullOrWhiteSpace(config.GameAssemblyPath) && !string.IsNullOrWhiteSpace(projectDir))
            {
                var p = Path.GetFullPath(Path.Combine(projectDir, config.GameAssemblyPath));
                if (File.Exists(p))
                    return p;
            }
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to resolve game assembly from project config");
        }

        return string.Empty;
    }
}