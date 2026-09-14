using ECS.Systems;

namespace Engine.Scene;

public static class RuntimeSceneStarter
{
    public static void Start(
        IScene scene,
        ISceneContext sceneContext,
        IEnumerable<IGameSystem> gameSystems)
    {
        sceneContext.SetState(SceneState.Play);
        scene.OnRuntimeStart(gameSystems);
    }
}
