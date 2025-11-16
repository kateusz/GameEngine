using System.Numerics;
using Engine.Renderer.Cameras;
using Engine.Scene;
using Engine.Scene.Components;
using Engine.Scene.Serializer;
using Engine.Scripting;
using Serilog;

namespace Editor.Panels;

public class SceneManager : ISceneManager, IEditorSceneManager, ISceneContext
{
    private static readonly ILogger Logger = Log.ForContext<SceneManager>();

    public SceneState SceneState { get; private set; } = SceneState.Edit;
    public string? EditorScenePath { get; private set; }
    public IScene? CurrentScene { get; private set; }

    // ISceneContext implementation
    public IScene? ActiveScene => CurrentScene;
    public SceneState State => SceneState;
    public event Action<IScene?, IScene?>? SceneChanged;

    private readonly Lazy<ISceneHierarchyPanel> _sceneHierarchyPanel;
    private readonly ISceneSerializer _sceneSerializer;
    private readonly SceneFactory _sceneFactory;
    private readonly IScriptEngine _scriptEngine;

    public SceneManager(Lazy<ISceneHierarchyPanel> sceneHierarchyPanel, ISceneSerializer sceneSerializer, SceneFactory sceneFactory, IScriptEngine scriptEngine)
    {
        _sceneHierarchyPanel = sceneHierarchyPanel;
        _sceneSerializer = sceneSerializer;
        _sceneFactory = sceneFactory;
        _scriptEngine = scriptEngine;
    }

    public void New(Vector2 viewportSize)
    {
        var oldScene = CurrentScene;

        // Dispose old scene before creating new one
        CurrentScene?.Dispose();

        CurrentScene = _sceneFactory.Create("");

        //CurrentScene.OnViewportResize((uint)viewportSize.X, (uint)viewportSize.Y);
        // SceneHierarchyPanel subscribes to SceneChanged event, so no need to call SetContext directly
        SceneChanged?.Invoke(oldScene, CurrentScene);
        Logger.Information("📄 New scene created");
    }

    public void Open(Vector2 viewportSize, string path)
    {
        if (SceneState != SceneState.Edit)
            Stop();

        var oldScene = CurrentScene;

        // Dispose old scene before loading new one
        CurrentScene?.Dispose();

        EditorScenePath = path;

        CurrentScene = _sceneFactory.Create(path);

        //CurrentScene.OnViewportResize((uint)viewportSize.X, (uint)viewportSize.Y);
        _sceneSerializer.Deserialize(CurrentScene, path);

        // SceneHierarchyPanel subscribes to SceneChanged event, so no need to call SetContext directly
        SceneChanged?.Invoke(oldScene, CurrentScene);
        Logger.Information("📂 Scene opened: {Path}", path);
    }

    public void Save(string? scenesDir)
    {
        var sceneDir = scenesDir ?? Path.Combine(Environment.CurrentDirectory, "assets", "scenes");
        if (!Directory.Exists(sceneDir))
            Directory.CreateDirectory(sceneDir);

        EditorScenePath = Path.Combine(sceneDir, "scene.scene");
        _sceneSerializer.Serialize(CurrentScene, EditorScenePath);
        Logger.Information("💾 Scene saved: {EditorScenePath}", EditorScenePath);
    }

    public void Play()
    {
        SceneState = SceneState.Play;
        CurrentScene.OnRuntimeStart();
        // SceneHierarchyPanel subscribes to SceneChanged event for UI updates
        Logger.Information("▶️ Scene play started");
    }

    public void Stop()
    {
        SceneState = SceneState.Edit;
        CurrentScene.OnRuntimeStop();
        // SceneHierarchyPanel subscribes to SceneChanged event for UI updates
        Logger.Information("⏹️ Scene play stopped");
    }

    public void Restart()
    {
        if (SceneState != SceneState.Play)
            return;

        CurrentScene.OnRuntimeStop();
        CurrentScene.OnRuntimeStart();
        // SceneHierarchyPanel subscribes to SceneChanged event for UI updates
        Logger.Information("🔄 Scene restarted");
    }

    public void DuplicateEntity()
    {
        if (SceneState != SceneState.Edit)
            return;

        var selectedEntity = _sceneHierarchyPanel.Value.GetSelectedEntity();
        if (selectedEntity is not null && CurrentScene != null)
        {
            CurrentScene.DuplicateEntity(selectedEntity);
            Logger.Information("📋 Entity duplicated: {EntityName}", selectedEntity.Name);
        }
    }

    public void FocusOnSelectedEntity(IOrthographicCameraController cameraController)
    {
        var selectedEntity = _sceneHierarchyPanel.Value.GetSelectedEntity();
        if (selectedEntity != null && selectedEntity.TryGetComponent<TransformComponent>(out var transform))
        {
            cameraController.SetPosition(transform.Translation);
        }
    }
}
