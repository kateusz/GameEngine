using ECS.Systems;
using Editor.Features.History;
using Editor.Features.Scripting;
using Editor.Scripting;
using Engine.Core;
using Engine.Project;
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
    GameScriptWorkspace scriptWorkspace,
    IEditorHistory history)
    : ISceneManager
{
    private static readonly ILogger Logger = Log.ForContext<SceneManager>();

    private string? _playSnapshotPath;
    private bool _playPaused;
    private string? _cleanJson;

    public string? EditorScenePath { get; private set; }

    public bool IsDirty =>
        _cleanJson is not null
        && sceneContext.ActiveScene is not null
        && sceneSerializer.SerializeToString(sceneContext.ActiveScene) != _cleanJson;

    public void New(string sceneName)
    {
        ReplaceActiveScene(sceneName);
        Logger.Information("📄 New scene created");

        if (!string.IsNullOrWhiteSpace(sceneName) && projectContext.HasProject)
            Save();

        CaptureCleanSnapshot();
    }

    public void Open(string path)
    {
        if (sceneContext.State != SceneState.Edit)
            Stop();

        ClearPlaySession();
        sceneContext.ActiveScene?.Dispose();
        EditorScenePath = null;

        EditorScenePath = path;
        var scene = sceneFactory.Create(Path.GetFileNameWithoutExtension(path));

        if (!string.IsNullOrEmpty(projectContext.ScriptsDir))
            scriptWorkspace.EnsureScriptsCompiledAndApplied();

        sceneSerializer.Deserialize(scene, path);
        sceneContext.SetScene(scene);
        CaptureCleanSnapshot();
        Logger.Information("📂 Scene opened: {Path}", path);
    }

    public void Save(bool compileScripts = true)
    {
        if (compileScripts && !string.IsNullOrEmpty(projectContext.ScriptsDir))
            scriptWorkspace.EnsureScriptsCompiledAndApplied();

        if (string.IsNullOrEmpty(EditorScenePath))
        {
            var sceneDir = PathBuilder.Build("scenes");
            Directory.CreateDirectory(sceneDir);
            EditorScenePath = Path.Combine(sceneDir, $"{sceneContext.ActiveScene!.Name}.scene");
        }

        sceneSerializer.Serialize(sceneContext.ActiveScene!, EditorScenePath);
        CaptureCleanSnapshot();
        Logger.Information("💾 Scene saved: {EditorScenePath}", EditorScenePath);
    }

    public void Close()
    {
        if (sceneContext.State == SceneState.Play)
            Stop();

        ReplaceActiveScene("");
        CaptureCleanSnapshot();
        Logger.Information("Scene closed");
    }

    public void Play()
    {
        if (string.IsNullOrEmpty(projectContext.Root) || projectContext.ScriptsDir is null)
        {
            Logger.Warning("No project or scripts directory — open a project before Play.");
            return;
        }

        var scene = sceneContext.ActiveScene!;
        var isResume = _playPaused && !string.IsNullOrEmpty(_playSnapshotPath) && File.Exists(_playSnapshotPath);

        if (!TryCompileAndLoadPlayAssembly(out _))
            return;

        if (!isResume)
        {
            history.Clear();
            _playSnapshotPath = Path.Combine(Path.GetTempPath(), $"ge-play-{Guid.NewGuid():N}.scene");
            sceneSerializer.Serialize(scene, _playSnapshotPath);
            ReloadEntitiesFromSnapshot(scene, _playSnapshotPath);
        }

        _playPaused = false;
        RuntimeSceneStarter.Start(scene, sceneContext, resolveGameSystems());
        Logger.Information(isResume ? "▶️ Scene play resumed" : "▶️ Scene play started");
    }

    public void Stop()
    {
        if (sceneContext.State != SceneState.Play)
            return;

        sceneContext.SetState(SceneState.Edit);
        sceneContext.ActiveScene?.OnRuntimeStop();
        scriptWorkspace.RestoreEditAssembly();
        _playPaused = true;

        Logger.Information("⏹️ Scene play stopped");
    }

    public void Restart()
    {
        if (string.IsNullOrEmpty(_playSnapshotPath) || !File.Exists(_playSnapshotPath))
        {
            Logger.Warning("Cannot restart scene: no play snapshot (enter play mode first)");
            return;
        }

        var scene = sceneContext.ActiveScene!;
        var wasPlaying = sceneContext.State == SceneState.Play;

        if (wasPlaying)
            scene.OnRuntimeStop();

        if (!TryCompileAndLoadPlayAssembly(out _))
            return;

        ReloadEntitiesFromSnapshot(scene, _playSnapshotPath);
        _playPaused = false;
        RuntimeSceneStarter.Start(scene, sceneContext, resolveGameSystems());
        Logger.Information("🔄 Scene restarted");
    }

    public string? GetCurrentScenePath() => EditorScenePath;

    private void ReplaceActiveScene(string sceneName)
    {
        ClearPlaySession();
        sceneContext.ActiveScene?.Dispose();
        EditorScenePath = null;
        sceneContext.SetScene(sceneFactory.Create(sceneName));
    }

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

    private bool TryCompileAndLoadPlayAssembly(out string[] buildErrors)
    {
        buildErrors = [];
        var engineDir = Path.Combine(projectContext.Root!, ".engine");
        Directory.CreateDirectory(engineDir);
        var dllPath = GameAssemblyCompiler.GetNextEditorBuildPath(engineDir);
        if (!GameAssemblyCompiler.TryCompile(projectContext.ScriptsDir!, dllPath, emitPdb: true, useDebugOptimization: true, out buildErrors))
        {
            foreach (var e in buildErrors)
                Logger.Error("Game script build: {Error}", e);
            return false;
        }

        scriptWorkspace.LoadGameAssemblyFromFile(dllPath, projectContext.ScriptsDir!);
        return true;
    }

    private void ClearPlaySession()
    {
        if (_playSnapshotPath is not null && File.Exists(_playSnapshotPath))
            File.Delete(_playSnapshotPath);

        _playSnapshotPath = null;
        _playPaused = false;
    }

    private void CaptureCleanSnapshot() =>
        _cleanJson = sceneContext.ActiveScene is null
            ? null
            : sceneSerializer.SerializeToString(sceneContext.ActiveScene);
}
