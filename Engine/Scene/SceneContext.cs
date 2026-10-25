using ECS.Systems;
using Engine.Project;
using Engine.Scene.Serializer;
using Engine.Scene.Systems;
using Serilog;

namespace Engine.Scene;

public class SceneContext(
    SceneFactory? sceneFactory = null,
    ISceneSerializer? sceneSerializer = null,
    Func<IEnumerable<IGameSystem>>? resolveGameSystems = null) : ISceneContext
{
    private static readonly ILogger Logger = Log.ForContext<SceneContext>();

    private string? _pendingPath;

    public IScene? ActiveScene { get; private set; }

    public PhysicsRuntimeBodyStore? ActivePhysicsBodyStore =>
        ActiveScene as Scene is { } scene ? scene.PhysicsBodies : null;

    public SceneState State { get; private set; } = SceneState.Edit;

    public event Action<IScene> SceneChanged = delegate { };

    public void SetScene(IScene newScene)
    {
        PointShadowCache.Clear();
        ActiveScene = newScene;
        SceneChanged.Invoke(newScene);
    }

    public void SetState(SceneState newState) => State = newState;

    public void RequestLoad(string scenePath)
    {
        if (string.IsNullOrWhiteSpace(scenePath))
            return;

        if (State != SceneState.Play)
        {
            Logger.Warning("RequestLoad ignored outside Play: {Path}", scenePath);
            return;
        }

        _pendingPath = scenePath;
    }

    public void FlushPendingLoad()
    {
        if (_pendingPath is null)
            return;

        var requested = _pendingPath;
        _pendingPath = null;

        if (State != SceneState.Play)
            return;

        if (sceneFactory is null || sceneSerializer is null || resolveGameSystems is null)
        {
            Logger.Warning("Cannot load scene — SceneContext is missing load dependencies.");
            return;
        }

        var resolved = ResolveExisting(requested);
        if (resolved is null)
        {
            Logger.Error("Scene not found: {Path}", requested);
            return;
        }

        IScene next;
        try
        {
            next = sceneFactory.Create(Path.GetFileNameWithoutExtension(resolved));
            sceneSerializer.Deserialize(next, resolved);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load scene: {Path}", resolved);
            return;
        }

        var previous = ActiveScene;
        previous?.OnRuntimeStop();
        previous?.Dispose();
        SetScene(next);
        RuntimeSceneStarter.Start(next, this, resolveGameSystems());
        Logger.Information("Loaded scene at runtime: {Path}", resolved);
    }

    private static string? ResolveExisting(string scenePath)
    {
        try
        {
            var viaAssets = PathBuilder.Resolve(scenePath);
            if (File.Exists(viaAssets))
                return viaAssets;
        }
        catch (InvalidOperationException)
        {
            // PathBuilder not wired — fall through.
        }

        var fromBase = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, scenePath));
        return File.Exists(fromBase) ? fromBase : null;
    }
}
