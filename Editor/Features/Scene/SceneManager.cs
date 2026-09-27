using System.Text.Json.Nodes;
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

    public void Open(string path) => Open(path, null);

    public void Open(string path, JsonObject? parsedRoot)
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

        if (parsedRoot is not null)
            sceneSerializer.Deserialize(scene, parsedRoot);
        else
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
            var sceneDir = PathBuilder.Resolve("scenes");
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

        // Compile before tearing down entities so a build failure leaves the scene intact.
        if (!TryCompilePlayAssembly(out var dllPath, out _))
            return;

        history.Clear();
        // Always snapshot live edit scene — Stop's Restart file must not skip this.
        if (string.IsNullOrEmpty(_playSnapshotPath) || !File.Exists(_playSnapshotPath))
            _playSnapshotPath = Path.Combine(Path.GetTempPath(), $"ge-play-{Guid.NewGuid():N}.scene");
        sceneSerializer.Serialize(scene, _playSnapshotPath);

        // Entities + IGameSystem instances pin the collectible ALC — drop them before unload.
        SwapPlayAssembly(scene, dllPath, _playSnapshotPath);

        RuntimeSceneStarter.Start(scene, sceneContext, resolveGameSystems());
        Logger.Information("▶️ Scene play started");
    }

    public void Stop()
    {
        if (sceneContext.State != SceneState.Play)
            return;

        sceneContext.SetState(SceneState.Edit);
        sceneContext.ActiveScene?.OnRuntimeStop();

        // Preserve restart snapshot — Open() → ClearPlaySession would delete the file.
        var playSnapshot = _playSnapshotPath;
        _playSnapshotPath = null;

        // Dispose / reload scene *before* any GameAssembly unload so live ALC roots are gone
        // (CORDBG_E_TARGET_INCONSISTENT when debugger is attached).
        if (!string.IsNullOrEmpty(EditorScenePath) && File.Exists(EditorScenePath))
        {
            Open(EditorScenePath);
            _playSnapshotPath = playSnapshot;
        }
        else
        {
            sceneContext.ActiveScene?.Dispose();
            scriptWorkspace.RestoreEditAssembly();
            sceneContext.SetScene(sceneFactory.Create(""));
            _playSnapshotPath = playSnapshot;
        }

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

        if (!TryCompilePlayAssembly(out var dllPath, out _))
            return;

        SwapPlayAssembly(scene, dllPath, _playSnapshotPath);
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

    private void SwapPlayAssembly(IScene scene, string dllPath, string snapshotPath)
    {
        if (sceneContext.State == SceneState.Play)
            scene.OnRuntimeStop();

        var destroyed = 0;
        foreach (var entity in scene.Entities.ToList())
        {
            scene.DestroyEntity(entity);
            destroyed++;
        }

        scriptWorkspace.LoadGameAssemblyFromFile(dllPath, projectContext.ScriptsDir!);
        sceneSerializer.Deserialize(scene, snapshotPath);
        Logger.Debug("♻️ Reloaded {Destroyed} entities from snapshot for play-mode assembly", destroyed);
    }

    private bool TryCompilePlayAssembly(out string dllPath, out string[] buildErrors)
    {
        dllPath = "";
        buildErrors = [];
        var engineDir = Path.Combine(projectContext.Root!, ".engine");
        Directory.CreateDirectory(engineDir);
        dllPath = GameAssemblyCompiler.GetNextEditorBuildPath(engineDir);
        if (!GameAssemblyCompiler.TryCompile(projectContext.ScriptsDir!, dllPath, emitPdb: true, useDebugOptimization: true, out buildErrors))
        {
            foreach (var e in buildErrors)
                Logger.Error("Game script build: {Error}", e);
            return false;
        }

        return true;
    }

    private void ClearPlaySession()
    {
        if (_playSnapshotPath is not null && File.Exists(_playSnapshotPath))
            File.Delete(_playSnapshotPath);

        _playSnapshotPath = null;
    }

    private void CaptureCleanSnapshot() =>
        _cleanJson = sceneContext.ActiveScene is null
            ? null
            : sceneSerializer.SerializeToString(sceneContext.ActiveScene);
}
