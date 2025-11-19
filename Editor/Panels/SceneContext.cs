using Engine.Scene;

namespace Editor.Panels;

public class SceneContext : ISceneContext
{
    public IScene? ActiveScene { get; private set; }

    public SceneState State { get; private set; } = SceneState.Edit;

    public event Action<IScene> SceneChanged;
    
    public void SetScene(IScene newScene)
    {
        ActiveScene = newScene;
        SceneChanged.Invoke(newScene);
    }
    
    public void SetState(SceneState state) => State = state;
}
