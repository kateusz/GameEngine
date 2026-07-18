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

        if (!string.IsNullOrWhiteSpace(sceneName) && projectContext.HasProject)
            Save();
    }

    public void Open(string path)
    {
        if (sceneContext.State != SceneState.Edit)
            Stop();

        sceneContext.ActiveScene?.Dispose();
        EditorScenePath = null;

        EditorScenePath = path;
        var scene = sceneFactory.Create(path, Path.GetFileNameWithoutExtension(path));

        if (!string.IsNullOrEmpty(projectContext.ScriptsDir))
            scriptWorkspace.EnsureScriptsCompiledAndApplied();

        sceneSerializer.Deserialize(scene, path);
        sceneContext.SetScene(scene);
        Logger.Information("📂 Scene opened: {Path}", path);
    }

    public void Save()
    {
        if (!string.IsNullOrEmpty(projectContext.ScriptsDir))
            scriptWorkspace.EnsureScriptsCompiledAndApplied();

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

        var scene = sceneContext.ActiveScene!;
        var snapshotPath = Path.Combine(Path.GetTempPath(), $"ge-play-{Guid.NewGuid():N}.scene");

        try
        {
            sceneSerializer.Serialize(scene, snapshotPath);

            var engineDir = Path.Combine(projectContext.Root, ".engine");
            Directory.CreateDirectory(engineDir);
            var dllPath = GameAssemblyCompiler.GetNextEditorBuildPath(engineDir);
            if (!GameAssemblyCompiler.TryCompile(projectContext.ScriptsDir, dllPath, emitPdb: true, useDebugOptimization: true, out var buildErrors))
            {
                foreach (var e in buildErrors)
                    Logger.Error("Game script build: {Error}", e);
                return;
            }

            scriptWorkspace.LoadGameAssemblyFromFile(dllPath, projectContext.ScriptsDir);

            ReloadEntitiesFromSnapshot(scene, snapshotPath);

            RuntimeSceneStarter.Start(scene, sceneContext, resolveGameSystems());
            Logger.Information("▶️ Scene play started");
        }
        finally
        {
            if (File.Exists(snapshotPath))
                File.Delete(snapshotPath);
        }
    }

    public void Stop()
    {
        sceneContext.SetState(SceneState.Edit);
        sceneContext.ActiveScene?.OnRuntimeStop();

        if (!string.IsNullOrEmpty(EditorScenePath) && File.Exists(EditorScenePath))
            Open(EditorScenePath);
        else
        {
            sceneContext.ActiveScene?.Dispose();
            scriptWorkspace.RestoreEditAssembly();
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

    private void ReloadEntitiesFromSnapshot(IScene scene, string snapshotPath)
    {
        var destroyed = 0;
        foreach (var entity in scene.Entities.ToList())
        {
            scene.DestroyEntity(entity);
            destroyed++;
        }

        sceneSerializer.Deserialize(scene, snapshotPath);
        Logger.Debug("♻️ Reloaded {Destroyed} entities from snapshot for play-mode assembly", destroyed);
    }
}
