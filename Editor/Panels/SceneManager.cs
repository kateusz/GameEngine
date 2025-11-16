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
    public IScene? CurrentScene { get; private set; }

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
        CurrentScene?.Dispose();

        CurrentScene = _sceneFactory.Create("");

        // Update script engine with the new scene
        _scriptEngine.SetCurrentScene(CurrentScene);

        //CurrentScene.OnViewportResize((uint)viewportSize.X, (uint)viewportSize.Y);
        _sceneHierarchyPanel.SetContext(CurrentScene);
        Logger.Information("📄 New scene created");
    }

    public void Open(Vector2 viewportSize, string path)
    {
        if (SceneState != SceneState.Edit)
            Stop();

        // Dispose old scene before loading new one
        CurrentScene?.Dispose();

        EditorScenePath = path;

        CurrentScene = _sceneFactory.Create(path);

        // Update script engine with the new scene
        _scriptEngine.SetCurrentScene(CurrentScene);

        //CurrentScene.OnViewportResize((uint)viewportSize.X, (uint)viewportSize.Y);
        _sceneHierarchyPanel.SetContext(CurrentScene);

        _sceneSerializer.Deserialize(CurrentScene, path);
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
        _sceneHierarchyPanel.SetContext(CurrentScene);
        Logger.Information("▶️ Scene play started");
    }

    public void Stop()
    {
        SceneState = SceneState.Edit;
        CurrentScene.OnRuntimeStop();
        _sceneHierarchyPanel.SetContext(CurrentScene);
        Logger.Information("⏹️ Scene play stopped");
    }

    public void Restart()
    {
        if (SceneState != SceneState.Play)
            return;

        CurrentScene.OnRuntimeStop();
        CurrentScene.OnRuntimeStart();
        _sceneHierarchyPanel.SetContext(CurrentScene);
        Logger.Information("🔄 Scene restarted");
    }

    public void DuplicateEntity()
    {
        if (SceneState != SceneState.Edit)
            return;

        var selectedEntity = _sceneHierarchyPanel.GetSelectedEntity();
        if (selectedEntity is not null && CurrentScene != null)
        {
            CurrentScene.DuplicateEntity(selectedEntity);
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
