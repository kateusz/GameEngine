using ECS.Systems;

namespace Engine.Scene;

public static class RuntimeSceneStarter
{
    public static void Start(
        IScene scene,
        ISceneContext sceneContext,
        IEnumerable<IGameSystem> gameSystems)
    {
        foreach (var system in gameSystems)
        {
            try
            {
                scene.RegisterRuntimeSystem(system);
            }
            catch (InvalidOperationException)
            {
                // Scene can re-enter play with already registered singleton instances.
            }
        }

        sceneContext.SetState(SceneState.Play);
        scene.OnRuntimeStart();
    }
}
