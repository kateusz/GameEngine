using ECS;
using ECS.Systems;
using Editor.Features.History;
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
    GameScriptWorkspace scriptWorkspace,
    IEditorHistory history)
    : ISceneManager
{
    private static readonly ILogger Logger = Log.ForContext<SceneManager>();

    private string? _playSnapshotPath;
    private bool _playPaused;
    private Action? _deferredRuntimeStart;
    private bool _pendingRuntimeStop;
    private string? _cleanSnapshotPath;

    private string? EditorScenePath { get; set; }

    public bool IsDirty =>
        _cleanSnapshotPath is not null
        && File.Exists(_cleanSnapshotPath)
        && sceneContext.ActiveScene is not null
        && SceneDiffersFromSnapshot();

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
        var dimension = sceneSerializer.PeekDimension(path);
        var scene = sceneFactory.Create(path, Path.GetFileNameWithoutExtension(path), dimension);

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
        {
            Stop();
            FlushPendingRuntimeStart();
        }

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
        var resume = isResume;
        _deferredRuntimeStart = () =>
        {
            RuntimeSceneStarter.Start(scene, sceneContext, resolveGameSystems());
            Logger.Information(resume ? "▶️ Scene play resumed" : "▶️ Scene play started");
        };
    }

    public void FlushPendingRuntimeStart()
    {
        if (_pendingRuntimeStop)
        {
            _pendingRuntimeStop = false;
            sceneContext.ActiveScene?.OnRuntimeStop();
            if (_deferredRuntimeStart is null)
            {
                scriptWorkspace.RestoreEditAssembly();
                Logger.Information("⏹️ Scene play stopped");
            }
        }

        if (_deferredRuntimeStart is null)
            return;

        var start = _deferredRuntimeStart;
        _deferredRuntimeStart = null;
        start();
    }

    public void Stop()
    {
        _deferredRuntimeStart = null;

        if (sceneContext.State != SceneState.Play)
            return;

        sceneContext.SetState(SceneState.Edit);
        _playPaused = true;
        _pendingRuntimeStop = true;
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
            _pendingRuntimeStop = true;

        if (!TryCompileAndLoadPlayAssembly(out _))
            return;

        ReloadEntitiesFromSnapshot(scene, _playSnapshotPath);
        _playPaused = false;
        _deferredRuntimeStart = () =>
        {
            RuntimeSceneStarter.Start(scene, sceneContext, resolveGameSystems());
            Logger.Information("🔄 Scene restarted");
        };
    }

    public void DuplicateEntity(Entity entity)
    {
        if (sceneContext.State != SceneState.Edit)
            return;

        sceneContext.ActiveScene?.DuplicateEntity(entity);
        Logger.Information("📋 Entity duplicated: {EntityName}", entity.Name);
    }

    public string? GetCurrentScenePath() => EditorScenePath;

    private void ReplaceActiveScene(string sceneName)
    {
        ClearPlaySession();
        sceneContext.ActiveScene?.Dispose();
        EditorScenePath = null;
        sceneContext.SetScene(sceneFactory.Create(path: "", sceneName));
    }

    private bool SceneDiffersFromSnapshot()
    {
        var checkPath = _cleanSnapshotPath + ".check";
        try
        {
            sceneSerializer.Serialize(sceneContext.ActiveScene!, checkPath);
            return !File.ReadAllBytes(_cleanSnapshotPath!).AsSpan()
                .SequenceEqual(File.ReadAllBytes(checkPath));
        }
        finally
        {
            if (File.Exists(checkPath))
                File.Delete(checkPath);
        }
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
        _deferredRuntimeStart = null;
        _pendingRuntimeStop = false;
    }

    private void CaptureCleanSnapshot()
    {
        DeleteCleanSnapshot();
        if (sceneContext.ActiveScene is null)
            return;

        _cleanSnapshotPath = Path.Combine(Path.GetTempPath(), $"ge-clean-{Guid.NewGuid():N}.scene");
        sceneSerializer.Serialize(sceneContext.ActiveScene, _cleanSnapshotPath);
    }

    private void DeleteCleanSnapshot()
    {
        if (_cleanSnapshotPath is not null && File.Exists(_cleanSnapshotPath))
            File.Delete(_cleanSnapshotPath);

        _cleanSnapshotPath = null;
    }
}
