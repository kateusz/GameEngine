using System.Numerics;
using Engine.Renderer.Cameras;
using Engine.Scene;
using Engine.Scene.Serializer;
using Serilog;

namespace Editor.Panels;

public class SceneManager : ISceneManager, IEditorSceneManager
{
    private static readonly ILogger Logger = Log.ForContext<SceneManager>();

    public string? EditorScenePath { get; private set; }

    private readonly ISceneContext _sceneContext;
    private readonly ISceneSerializer _sceneSerializer;
    private readonly SceneFactory _sceneFactory;

    public SceneManager(ISceneContext sceneContext, ISceneSerializer sceneSerializer, SceneFactory sceneFactory)
    {
        _sceneContext = sceneContext;
        _sceneSerializer = sceneSerializer;
        _sceneFactory = sceneFactory;
    }

    public void New(Vector2 viewportSize)
    {
        _sceneContext.ActiveScene?.Dispose();

        _sceneContext.SetScene(_sceneFactory.Create(""));
        Logger.Information("📄 New scene created");
    }

    public void Open(Vector2 viewportSize, string path)
    {
        if (_sceneContext.State != SceneState.Edit)
            Stop();
        
        _sceneContext.ActiveScene?.Dispose();

        EditorScenePath = path;
        _sceneContext.SetScene(_sceneFactory.Create(path));
        _sceneSerializer.Deserialize(_sceneContext.ActiveScene!, path);
        Logger.Information("📂 Scene opened: {Path}", path);
    }

    public void Save(string? scenesDir)
    {
        var sceneDir = scenesDir ?? Path.Combine(Environment.CurrentDirectory, "assets", "scenes");
        if (!Directory.Exists(sceneDir))
            Directory.CreateDirectory(sceneDir);

        EditorScenePath = Path.Combine(sceneDir, "scene.scene");
        _sceneSerializer.Serialize(_sceneContext.ActiveScene, EditorScenePath);
        Logger.Information("💾 Scene saved: {EditorScenePath}", EditorScenePath);
    }

    public void Play()
    {
        _sceneContext.SetState(SceneState.Play);
        _sceneContext.ActiveScene.OnRuntimeStart();
        Logger.Information("▶️ Scene play started");
    }

    public void Stop()
    {
        _sceneContext.SetState(SceneState.Edit);
        _sceneContext.ActiveScene.OnRuntimeStop();
        Logger.Information("⏹️ Scene play stopped");
    }

    public void Restart()
    {
        if (_sceneContext.State != SceneState.Play)
            return;

        _sceneContext.ActiveScene.OnRuntimeStop();
        _sceneContext.ActiveScene.OnRuntimeStart();
        Logger.Information("🔄 Scene restarted");
    }

    public void DuplicateEntity()
    {
        if (_sceneContext.State != SceneState.Edit)
            return;

        // var selectedEntity = _sceneHierarchyPanel.Value.GetSelectedEntity();
        // if (selectedEntity is not null && CurrentScene != null)
        // {
        //     CurrentScene.DuplicateEntity(selectedEntity);
        //     Logger.Information("📋 Entity duplicated: {EntityName}", selectedEntity.Name);
        // }
    }

    public void FocusOnSelectedEntity(IOrthographicCameraController cameraController)
    {
        // var selectedEntity = _sceneHierarchyPanel.Value.GetSelectedEntity();
        // if (selectedEntity != null && selectedEntity.TryGetComponent<TransformComponent>(out var transform))
        // {
        //     cameraController.SetPosition(transform.Translation);
        // }
    }
}