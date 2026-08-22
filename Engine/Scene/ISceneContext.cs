using Engine.Scene.Systems;

namespace Engine.Scene;

public interface ISceneContext
{
    IScene? ActiveScene { get; }
    ScriptRuntimeStore? ActiveScriptRuntimeStore { get; }
    PhysicsRuntimeBodyStore? ActivePhysicsBodyStore { get; }
    SceneState State { get; }
    event Action<IScene> SceneChanged;
    void SetScene(IScene newScene);
    void SetState(SceneState newState);
}
