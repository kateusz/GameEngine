using ECS;
using Engine.Scene;
using Engine.Scene.Serializer;

namespace Editor.Managers;

public class SceneController
{
    private SceneState _sceneState = SceneState.Edit;
    private string? _editorScenePath;

    public SceneState CurrentState => _sceneState;
    public string? EditorScenePath => _editorScenePath;

    public void NewScene()
    {
        CurrentScene.Set(new Scene(""));
        OnSceneChanged?.Invoke();
        Console.WriteLine("📄 New scene created");
    }

    public void OpenScene()
    {
        if (_sceneState != SceneState.Edit)
            StopScene();
        
        const string filePath = "assets/scenes/Example.scene";
        
        if (string.IsNullOrWhiteSpace(filePath))
            throw new Exception($"Scene doesn't exist: {filePath}");
        
        LoadScene(filePath);
    }

    public void OpenScene(string path)
    {
        if (_sceneState != SceneState.Edit)
            StopScene();

        LoadScene(path);
    }

    private void LoadScene(string path)
    {
        _editorScenePath = path;
        CurrentScene.Set(new Scene(path));
        
        SceneSerializer.Deserialize(CurrentScene.Instance, path);
        OnSceneChanged?.Invoke();
        Console.WriteLine($"📂 Scene opened: {path}");
    }
    
    public void SaveScene(string? currentProjectDirectory = null)
    {
        string sceneDir;
        if (!string.IsNullOrEmpty(currentProjectDirectory))
        {
            sceneDir = Path.Combine(currentProjectDirectory, "assets", "scenes");
        }
        else
        {
            sceneDir = Path.Combine(Environment.CurrentDirectory, "assets", "scenes");
        }
        
        if (!Directory.Exists(sceneDir))
            Directory.CreateDirectory(sceneDir);
            
        _editorScenePath = Path.Combine(sceneDir, "scene.scene");
        
        if (!string.IsNullOrWhiteSpace(_editorScenePath))
        {
            SceneSerializer.Serialize(CurrentScene.Instance, _editorScenePath);
            Console.WriteLine($"💾 Scene saved: {_editorScenePath}");
        }
    }

    public void PlayScene()
    {
        if (_sceneState == SceneState.Play) return;
        
        _sceneState = SceneState.Play;
        CurrentScene.Instance.OnRuntimeStart();
        OnSceneStateChanged?.Invoke(_sceneState);
        Console.WriteLine("▶️ Scene play started");
    }

    public void StopScene()
    {
        if (_sceneState == SceneState.Edit) return;
        
        _sceneState = SceneState.Edit;
        CurrentScene.Instance.OnRuntimeStop();
        OnSceneStateChanged?.Invoke(_sceneState);
        Console.WriteLine("⏹️ Scene play stopped");
    }

    public void UpdateScene(TimeSpan timeSpan, object camera)
    {
        switch (_sceneState)
        {
            case SceneState.Edit:
                if (camera is Engine.Renderer.Cameras.OrthographicCamera orthoCamera)
                    CurrentScene.Instance.OnUpdateEditor(timeSpan, orthoCamera);
                break;
            case SceneState.Play:
                CurrentScene.Instance.OnUpdateRuntime(timeSpan);
                break;
        }
    }

    public void OnViewportResize(uint width, uint height)
    {
        CurrentScene.Instance.OnViewportResize(width, height);
    }

    public void DuplicateEntity(Entity? selectedEntity)
    {
        if (_sceneState != SceneState.Edit || selectedEntity is null)
            return;
        
        CurrentScene.Instance.DuplicateEntity(selectedEntity);
        Console.WriteLine($"📋 Entity duplicated: {selectedEntity.Name}");
    }

    // Events
    public event Action? OnSceneChanged;
    public event Action<SceneState>? OnSceneStateChanged;
}