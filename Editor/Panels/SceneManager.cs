using System.Numerics;
using Engine.Renderer;
using Engine.Renderer.Cameras;
using Engine.Scene;
using Engine.Scene.Components;
using Engine.Scene.Serializer;

namespace Editor.Panels;

public class SceneManager
{
    public SceneState SceneState { get; private set; } = SceneState.Edit;
    public string? EditorScenePath { get; private set; }

    private readonly SceneHierarchyPanel _sceneHierarchyPanel;
    private readonly IGraphics2D _graphics2D;
    private readonly IGraphics3D _graphics3D;

    public SceneManager(SceneHierarchyPanel sceneHierarchyPanel, IGraphics2D graphics2D, IGraphics3D graphics3D)
    {
        _sceneHierarchyPanel = sceneHierarchyPanel;
        _graphics2D = graphics2D;
        _graphics3D = graphics3D;
    }

    public void New()
    {
        CurrentScene.Set(new Scene("", _graphics2D, _graphics3D));
        _sceneHierarchyPanel.SetContext(CurrentScene.Instance);
        Console.WriteLine("📄 New scene created");
    }

    public void Open(string path)
    {
        if (SceneState != SceneState.Edit)
            Stop();

        EditorScenePath = path;
        CurrentScene.Set(new Scene(path,  _graphics2D, _graphics3D));
        _sceneHierarchyPanel.SetContext(CurrentScene.Instance);

        SceneSerializer.Deserialize(CurrentScene.Instance, path);
        Console.WriteLine($"📂 Scene opened: {path}");
    }

    public void Save(string? scenesDir)
    {
        var sceneDir = scenesDir ?? Path.Combine(Environment.CurrentDirectory, "assets", "scenes");
        if (!Directory.Exists(sceneDir))
            Directory.CreateDirectory(sceneDir);

        EditorScenePath = Path.Combine(sceneDir, "scene.scene");
        SceneSerializer.Serialize(CurrentScene.Instance, EditorScenePath);
        Console.WriteLine($"💾 Scene saved: {EditorScenePath}");
    }

    public void Play()
    {
        SceneState = SceneState.Play;
        CurrentScene.Instance.OnRuntimeStart();
        _sceneHierarchyPanel.SetContext(CurrentScene.Instance);
        Console.WriteLine("▶️ Scene play started");
    }

    public void Stop()
    {
        SceneState = SceneState.Edit;
        CurrentScene.Instance.OnRuntimeStop();
        _sceneHierarchyPanel.SetContext(CurrentScene.Instance);
        Console.WriteLine("⏹️ Scene play stopped");
    }

    public void DuplicateEntity()
    {
        if (SceneState != SceneState.Edit)
            return;

        var selectedEntity = _sceneHierarchyPanel.GetSelectedEntity();
        if (selectedEntity is not null)
        {
            CurrentScene.Instance.DuplicateEntity(selectedEntity);
            Console.WriteLine($"📋 Entity duplicated: {selectedEntity.Name}");
        }
    }

    public void FocusOnSelectedEntity(OrthographicCameraController cameraController)
    {
        var selectedEntity = _sceneHierarchyPanel.GetSelectedEntity();
        if (selectedEntity != null && selectedEntity.HasComponent<TransformComponent>())
        {
            var transform = selectedEntity.GetComponent<TransformComponent>();
            cameraController.Camera.SetPosition(transform.Translation);
        }
    }
}
