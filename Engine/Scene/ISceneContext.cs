using Engine.Scene.Systems;

namespace Engine.Scene;

public interface ISceneContext
{
    IScene? ActiveScene { get; }
    PhysicsRuntimeBodyStore? ActivePhysicsBodyStore { get; }
    SceneState State { get; }
    event Action<IScene> SceneChanged;
    void SetScene(IScene newScene);
    void SetState(SceneState newState);

    /// <summary>Queue a scene swap after the current frame finishes updating (Play only).</summary>
    void RequestLoad(string scenePath);

    /// <summary>Apply a pending <see cref="RequestLoad"/> if any.</summary>
    void FlushPendingLoad();
}
