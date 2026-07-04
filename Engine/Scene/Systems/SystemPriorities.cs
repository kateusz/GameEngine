namespace Engine.Scene.Systems;

/// <summary>
/// 
/// </summary>
public static class SystemPriorities
{
    public const int PhysicsSimulationSystem = 100;
    public const int ScriptUpdateSystem = 110;
    public const int AudioSystem = 120;
    public const int AnimationSystem = 140;
    public const int PrimaryCameraSystem = 145;
    public const int SceneRenderSystem = 150;
    public const int PhysicsDebugRenderSystem = 151;
}