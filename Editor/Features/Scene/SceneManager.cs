using ECS;
using Engine.Scene;
using Engine.Scene.Serializer;
using Serilog;

namespace Editor.Features.Scene;

public class SceneManager(ISceneContext sceneContext, ISceneSerializer sceneSerializer, SceneFactory sceneFactory)
    : ISceneManager
{
    private static readonly ILogger Logger = Log.ForContext<SceneManager>();

    public string? EditorScenePath { get; private set; }

    public void New()
    {
        sceneContext.ActiveScene?.Dispose();

        sceneContext.SetScene(sceneFactory.Create(""));
        Logger.Information("📄 New scene created");
    }

    public void Open(string path)
    {
        if (sceneContext.State != SceneState.Edit)
            Stop();

        sceneContext.ActiveScene.Dispose();

        EditorScenePath = path;
        sceneContext.SetScene(sceneFactory.Create(path));
        sceneSerializer.Deserialize(sceneContext.ActiveScene!, path);
        Logger.Information("📂 Scene opened: {Path}", path);
    }

    public void Save(string? scenesDir)
    {
        var sceneDir = scenesDir ?? Path.Combine(Environment.CurrentDirectory, "assets", "scenes");
        if (!Directory.Exists(sceneDir))
            Directory.CreateDirectory(sceneDir);

        EditorScenePath = Path.Combine(sceneDir, "scene.scene");
        sceneSerializer.Serialize(sceneContext.ActiveScene, EditorScenePath);
        Logger.Information("💾 Scene saved: {EditorScenePath}", EditorScenePath);
    }

    public void Play()
    {
        sceneContext.SetState(SceneState.Play);
        sceneContext.ActiveScene.OnRuntimeStart();
        Logger.Information("▶️ Scene play started");
    }

    public void Stop()
    {
        sceneContext.SetState(SceneState.Edit);
        sceneContext.ActiveScene.OnRuntimeStop();
        Logger.Information("⏹️ Scene play stopped");
    }

    public void Restart()
    {
        Open(EditorScenePath!);
        //_sceneContext.ActiveScene.OnRuntimeStart();
        Logger.Information("🔄 Scene restarted");
    }

    public void DuplicateEntity(Entity entity)
    {
        if (sceneContext.State != SceneState.Edit)
            return;

        sceneContext.ActiveScene?.DuplicateEntity(entity);
        Logger.Information("📋 Entity duplicated: {EntityName}", entity.Name);
    }

    public string? GetCurrentScenePath() => EditorScenePath;
}