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
    private readonly ICurrentScene _currentScene;

    public SceneManager(ISceneHierarchyPanel sceneHierarchyPanel, ISceneSerializer sceneSerializer, SceneFactory sceneFactory, IScriptEngine scriptEngine, ICurrentScene currentScene)
    {
        _sceneHierarchyPanel = sceneHierarchyPanel;
        _sceneSerializer = sceneSerializer;
        _sceneFactory = sceneFactory;
        _scriptEngine = scriptEngine;
        _currentScene = currentScene;
    }

    public void New(Vector2 viewportSize)
    {
        // Dispose old scene before creating new one
        _currentScene.Instance?.Dispose();

        var scene = _sceneFactory.Create("");
        _currentScene.Set(scene);

        // Update script engine with the new scene
        _scriptEngine.SetCurrentScene(scene);

        //_currentScene.Instance.OnViewportResize((uint)viewportSize.X, (uint)viewportSize.Y);
        _sceneHierarchyPanel.SetContext(_currentScene.Instance);
        Logger.Information("📄 New scene created");
    }

    public void Open(Vector2 viewportSize, string path)
    {
        if (SceneState != SceneState.Edit)
            Stop();

        // Dispose old scene before loading new one
        _currentScene.Instance?.Dispose();

        EditorScenePath = path;

        var scene = _sceneFactory.Create(path);
        _currentScene.Set(scene);

        // Update script engine with the new scene
        _scriptEngine.SetCurrentScene(scene);

        //_currentScene.Instance.OnViewportResize((uint)viewportSize.X, (uint)viewportSize.Y);
        _sceneHierarchyPanel.SetContext(_currentScene.Instance);

        _sceneSerializer.Deserialize(_currentScene.Instance, path);
        Logger.Information("📂 Scene opened: {Path}", path);
    }

    public void Save(string? scenesDir)
    {
        var sceneDir = scenesDir ?? Path.Combine(Environment.CurrentDirectory, "assets", "scenes");
        if (!Directory.Exists(sceneDir))
            Directory.CreateDirectory(sceneDir);

        EditorScenePath = Path.Combine(sceneDir, "scene.scene");
        _sceneSerializer.Serialize(_currentScene.Instance, EditorScenePath);
        Logger.Information("💾 Scene saved: {EditorScenePath}", EditorScenePath);
    }

    public void Play()
    {
        SceneState = SceneState.Play;
        _currentScene.Instance.OnRuntimeStart();
        _sceneHierarchyPanel.SetContext(_currentScene.Instance);
        Logger.Information("▶️ Scene play started");
    }

    public void Stop()
    {
        SceneState = SceneState.Edit;
        _currentScene.Instance.OnRuntimeStop();
        _sceneHierarchyPanel.SetContext(_currentScene.Instance);
        Logger.Information("⏹️ Scene play stopped");
    }

    public void DuplicateEntity()
    {
        if (SceneState != SceneState.Edit)
            return;

        var selectedEntity = _sceneHierarchyPanel.GetSelectedEntity();
        if (selectedEntity is not null && _currentScene.Instance != null)
        {
            _currentScene.Instance.DuplicateEntity(selectedEntity);
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
