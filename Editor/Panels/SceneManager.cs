using System.Numerics;
using Engine.Renderer.Cameras;
using Engine.Scene;
using Engine.Scene.Components;
using Engine.Scene.Serializer;
using Serilog;

namespace Editor.Panels;

public class SceneManager
{
    private static readonly Serilog.ILogger Logger = Log.ForContext<SceneManager>();
    
    public SceneState SceneState { get; private set; } = SceneState.Edit;
    public string? EditorScenePath { get; private set; }

    private readonly SceneHierarchyPanel _sceneHierarchyPanel;
    private readonly ISceneSerializer _sceneSerializer;

    public SceneManager(SceneHierarchyPanel sceneHierarchyPanel, ISceneSerializer sceneSerializer)
    {
        _sceneHierarchyPanel = sceneHierarchyPanel;
        _sceneSerializer = sceneSerializer;
    }

    public void New(Vector2 viewportSize)
    {
        CurrentScene.Set(new Scene(""));
        //CurrentScene.Instance.OnViewportResize((uint)viewportSize.X, (uint)viewportSize.Y);
        _sceneHierarchyPanel.SetContext(CurrentScene.Instance);
        Logger.Information("📄 New scene created");
    }

    public void Open(Vector2 viewportSize, string path)
    {
        if (SceneState != SceneState.Edit)
            Stop();

        EditorScenePath = path;
        CurrentScene.Set(new Scene(path));
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
        SceneState = SceneState.Play;
        CurrentScene.Instance.OnRuntimeStart();
        _sceneHierarchyPanel.SetContext(CurrentScene.Instance);
        Logger.Information("▶️ Scene play started");
    }

    public void Stop()
    {
        SceneState = SceneState.Edit;
        CurrentScene.Instance.OnRuntimeStop();
        _sceneHierarchyPanel.SetContext(CurrentScene.Instance);
        Logger.Information("⏹️ Scene play stopped");
    }

    public void DuplicateEntity()
    {
        if (SceneState != SceneState.Edit)
            return;

        var selectedEntity = _sceneHierarchyPanel.GetSelectedEntity();
        if (selectedEntity is not null)
        {
            CurrentScene.Instance.DuplicateEntity(selectedEntity);
            Logger.Information("📋 Entity duplicated: {EntityName}", selectedEntity.Name);
        }
    }

    public void FocusOnSelectedEntity(OrthographicCameraController cameraController)
    {
        var selectedEntity = _sceneHierarchyPanel.GetSelectedEntity();
        if (selectedEntity != null && selectedEntity.TryGetComponent<TransformComponent>(out var transform))
        {
            cameraController.Camera.SetPosition(transform.Translation);
        }
    }
}
