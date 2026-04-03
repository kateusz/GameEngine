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
    public const int SpriteRenderSystem = 150;
    public const int SubTextureRenderSystem = 160;
    public const int LightingSystem = 165;
    public const int ModelRenderSystem = 170;
    public const int PhysicsDebugRenderSystem = 180;
    
}