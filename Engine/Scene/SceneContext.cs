using Engine.Scene.Systems;

namespace Engine.Scene;

public class SceneContext : ISceneContext
{
    public IScene? ActiveScene { get; private set; }

    public PhysicsRuntimeBodyStore? ActivePhysicsBodyStore =>
        ActiveScene as Scene is { } scene ? scene.PhysicsBodies : null;

    public SceneState State { get; private set; } = SceneState.Edit;

    public event Action<IScene> SceneChanged = delegate { };
    
    public void SetScene(IScene newScene)
    {
        ActiveScene = newScene;
        SceneChanged.Invoke(newScene);
    }
    
    public void SetState(SceneState newState) => State = newState;
}
