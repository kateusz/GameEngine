using Engine.Scene.Systems;

namespace Engine.Scene;

public class SceneContext : ISceneContext
{
    public IScene? ActiveScene { get; private set; }

    public ScriptRuntimeStore? ActiveScriptRuntimeStore =>
        ActiveScene as Scene is { } scene ? scene.ScriptRuntimeStore : null;

    public PhysicsRuntimeBodyStore? ActivePhysicsBodyStore =>
        ActiveScene as Scene is { } scene ? scene.PhysicsBodies : null;

    public PhysicsRuntimeBodyStore3D? ActivePhysicsBodyStore3D =>
        ActiveScene as Scene is { } scene ? scene.PhysicsBodies3D : null;

    public SceneState State { get; private set; } = SceneState.Edit;

    public event Action<IScene> SceneChanged;
    
    public void SetScene(IScene newScene)
    {
        ActiveScene = newScene;
        SceneChanged.Invoke(newScene);
    }
    
    public void SetState(SceneState newState) => State = newState;
}
