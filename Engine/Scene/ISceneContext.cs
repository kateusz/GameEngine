namespace Engine.Scene;

public interface ISceneContext
{
    IScene ActiveScene { get; }
    ScriptRuntimeStore? ActiveScriptRuntimeStore { get; }
    SceneState State { get; }
    event Action<IScene> SceneChanged;
    void SetScene(IScene newScene);
    void SetState(SceneState newState);
}
