using System.Numerics;
using Engine.Renderer.Cameras;
using Engine.Scene;
using Engine.Scene.Components;
using Engine.Scene.Serializer;
using Engine.Scripting;
using Serilog;

namespace Editor.Panels;

public class SceneManager : ISceneManager
{
    private static readonly ILogger Logger = Log.ForContext<SceneManager>();

    public SceneState SceneState { get; private set; } = SceneState.Edit;
    public string? EditorScenePath { get; private set; }

    private readonly ISceneHierarchyPanel _sceneHierarchyPanel;
    private readonly ISceneSerializer _sceneSerializer;
    private readonly SceneFactory _sceneFactory;
    private readonly IScriptEngine _scriptEngine;

    public SceneManager(ISceneHierarchyPanel sceneHierarchyPanel, ISceneSerializer sceneSerializer, SceneFactory sceneFactory, IScriptEngine scriptEngine)
    {
        _sceneHierarchyPanel = sceneHierarchyPanel;
        _sceneSerializer = sceneSerializer;
        _sceneFactory = sceneFactory;
        _scriptEngine = scriptEngine;
    }

    public void New(Vector2 viewportSize)
    {
        // Dispose old scene before creating new one
        CurrentScene.Instance?.Dispose();

        var scene = _sceneFactory.Create("");
        CurrentScene.Set(scene);

        // Update script engine with the new scene
        _scriptEngine.SetCurrentScene(scene);

        //CurrentScene.Instance.OnViewportResize((uint)viewportSize.X, (uint)viewportSize.Y);
        _sceneHierarchyPanel.SetContext(CurrentScene.Instance);
        Logger.Information("📄 New scene created");
    }

    public void Open(Vector2 viewportSize, string path)
    {
        if (SceneState != SceneState.Edit)
            Stop();

        // Dispose old scene before loading new one
        CurrentScene.Instance?.Dispose();

        EditorScenePath = path;

        var scene = _sceneFactory.Create(path);
        CurrentScene.Set(scene);

        // Update script engine with the new scene
        _scriptEngine.SetCurrentScene(scene);

        //CurrentScene.Instance.OnViewportResize((uint)viewportSize.X, (uint)viewportSize.Y);
        _sceneHierarchyPanel.SetContext(CurrentScene.Instance);

        _sceneSerializer.Deserialize(CurrentScene.Instance, path);
        Logger.Information("📂 Scene opened: {Path}", path);
    }

    public void Save(string? scenesDir)
    {
        var sceneDir = scenesDir ?? Path.Combine(Environment.CurrentDirectory, "assets", "scenes");
        if (!Directory.Exists(sceneDir))
            Directory.CreateDirectory(sceneDir);

        EditorScenePath = Path.Combine(sceneDir, "scene.scene");
        _sceneSerializer.Serialize(CurrentScene.Instance, EditorScenePath);
        Logger.Information("💾 Scene saved: {EditorScenePath}", EditorScenePath);
    }

    public void Play()
    {
        if (SceneState == SceneState.Play) return;

        // If resuming from pause, just change state
        if (SceneState == SceneState.Paused)
        {
            Resume();
            return;
        }

        // Capture snapshot before starting runtime
        CurrentScene.Instance?.CaptureSnapshot(_sceneSerializer);

        // Reset and start time tracking
        Engine.Core.Time.Reset();
        Engine.Core.Time.TimeScale = 1.0f;

        SceneState = SceneState.Play;
        CurrentScene.Instance?.OnRuntimeStart();
        _sceneHierarchyPanel.SetContext(CurrentScene.Instance);

        Logger.Information("▶️  Scene play started");
    }

    public void Pause()
    {
        if (SceneState != SceneState.Play) return;

        SceneState = SceneState.Paused;
        Engine.Core.Time.TimeScale = 0.0f; // Freeze time

        // Notify script engine to pause scripts
        _scriptEngine.Pause();

        Logger.Information("⏸️  Scene paused");
    }

    public void Resume()
    {
        if (SceneState != SceneState.Paused) return;

        SceneState = SceneState.Play;
        Engine.Core.Time.TimeScale = 1.0f; // Resume time

        // Notify script engine to resume scripts
        _scriptEngine.Resume();

        Logger.Information("▶️  Scene resumed");
    }

    public void Stop()
    {
        if (SceneState == SceneState.Edit) return;

        // Stop runtime systems
        CurrentScene.Instance?.OnRuntimeStop();

        // Restore scene to pre-play snapshot
        CurrentScene.Instance?.RestoreFromSnapshot(_sceneSerializer);

        // Clear snapshot to free memory
        CurrentScene.Instance?.ClearSnapshot();

        // Reset time
        Engine.Core.Time.Reset();

        SceneState = SceneState.Edit;
        _sceneHierarchyPanel.SetContext(CurrentScene.Instance);

        Logger.Information("⏹️  Scene stopped and restored to initial state");
    }

    public void Restart()
    {
        if (SceneState == SceneState.Edit) return;

        Logger.Information("🔄 Restarting scene...");

        // Stop runtime
        CurrentScene.Instance?.OnRuntimeStop();

        // Restore from snapshot
        CurrentScene.Instance?.RestoreFromSnapshot(_sceneSerializer);

        // Re-capture snapshot (in case scene was modified)
        CurrentScene.Instance?.CaptureSnapshot(_sceneSerializer);

        // Reset time
        Engine.Core.Time.Reset();
        Engine.Core.Time.TimeScale = 1.0f;

        // Restart runtime
        SceneState = SceneState.Play;
        CurrentScene.Instance?.OnRuntimeStart();
        _sceneHierarchyPanel.SetContext(CurrentScene.Instance);

        Logger.Information("🔄 Scene restarted");
    }

    public void DuplicateEntity()
    {
        if (SceneState != SceneState.Edit)
            return;

        var selectedEntity = _sceneHierarchyPanel.GetSelectedEntity();
        if (selectedEntity is not null && CurrentScene.Instance != null)
        {
            CurrentScene.Instance.DuplicateEntity(selectedEntity);
            Logger.Information("📋 Entity duplicated: {EntityName}", selectedEntity.Name);
        }
    }

    public void FocusOnSelectedEntity(IOrthographicCameraController cameraController)
    {
        var selectedEntity = _sceneHierarchyPanel.GetSelectedEntity();
        if (selectedEntity != null && selectedEntity.TryGetComponent<TransformComponent>(out var transform))
        {
            cameraController.SetPosition(transform.Translation);
        }
    }
}
