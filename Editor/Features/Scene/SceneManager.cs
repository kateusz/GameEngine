using ECS;
using ECS.Systems;
using Editor.Features.Scripting;
using Engine.Core;
using Engine.Scene;
using Engine.Scene.Serializer;
using Engine.Scripting;
using Serilog;

namespace Editor.Features.Scene;

public class SceneManager(
    ISceneContext sceneContext,
    ISceneSerializer sceneSerializer,
    SceneFactory sceneFactory,
    Func<IEnumerable<IGameSystem>> resolveGameSystems,
    IProjectContext projectContext,
    IGameAssemblyBuilder gameAssemblyBuilder,
    IScriptEngine scriptEngine,
    GameScriptWorkspace scriptWorkspace)
    : ISceneManager
{
    private static readonly ILogger Logger = Log.ForContext<SceneManager>();

    public string? EditorScenePath { get; private set; }

    public void New(string sceneName)
    {
        sceneContext.ActiveScene?.Dispose();
        EditorScenePath = null;

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

        if (!string.IsNullOrEmpty(projectContext.ScriptsDir))
            scriptWorkspace.EnsureScriptsCompiledAndApplied();

        sceneSerializer.Deserialize(sceneContext.ActiveScene!, path);
        Logger.Information("📂 Scene opened: {Path}", path);
    }

    public void Save()
    {
        if (string.IsNullOrEmpty(EditorScenePath))
        {
            var sceneDir = PathBuilder.Build("scenes");
            Directory.CreateDirectory(sceneDir);
            EditorScenePath = Path.Combine(sceneDir, $"{sceneContext.ActiveScene!.Name}.scene");
        }

        sceneSerializer.Serialize(sceneContext.ActiveScene!, EditorScenePath);
        Logger.Information("💾 Scene saved: {EditorScenePath}", EditorScenePath);
    }

    public void Play()
    {
        if (string.IsNullOrEmpty(projectContext.Root) || projectContext.ScriptsDir is null)
        {
            Logger.Warning("No project or scripts directory — open a project before Play.");
            return;
        }

        var engineDir = Path.Combine(projectContext.Root, ".engine");
        Directory.CreateDirectory(engineDir);
        var dllPath = GameAssemblyCompiler.GetNextEditorBuildPath(engineDir);
        if (!gameAssemblyBuilder.TryBuild(projectContext.ScriptsDir, dllPath, emitPdb: true, out var buildErrors))
        {
            foreach (var e in buildErrors)
                Logger.Error("Game script build: {Error}", e);
            return;
        }

        scriptWorkspace.LoadGameAssemblyFromFile(dllPath, projectContext.ScriptsDir);
        scriptEngine.SetSuppressFileChangeRecompile(true);

        if (!string.IsNullOrEmpty(EditorScenePath))
            sceneSerializer.Serialize(sceneContext.ActiveScene!, EditorScenePath);

        RuntimeSceneStarter.Start(
            sceneContext.ActiveScene!,
            sceneContext,
            resolveGameSystems());
        Logger.Information("▶️ Scene play started");
    }

    public void Stop()
    {
        sceneContext.SetState(SceneState.Edit);
        sceneContext.ActiveScene.OnRuntimeStop();
        scriptEngine.SetSuppressFileChangeRecompile(false);
        if (projectContext.ScriptsDir is { } scriptsDir && projectContext.Root is { } projectDir)
            scriptWorkspace.SetScriptsDirectory(scriptsDir, GameScriptWorkspace.ResolveEditorDllPath(projectDir));

        if (!string.IsNullOrEmpty(EditorScenePath) && File.Exists(EditorScenePath))
            Open(EditorScenePath);

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
}
