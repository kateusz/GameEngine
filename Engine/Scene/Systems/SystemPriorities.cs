using Engine.Core;

namespace Engine.Scene.Systems;

[SkipUnitTests]
public static class SystemPriorities
{
    public const int PhysicsSimulationSystem = 100;
    public const int ScriptUpdateSystem = 110;
    public const int TransformHierarchySystem = 115;
    public const int AudioSystem = 120;
    public const int SkeletalAnimationSystem = 135;
    public const int PrimaryCameraSystem = 145;
    public const int SceneRenderSystem = 150;
    public const int PhysicsDebugRenderSystem = 151;
    public const int PaperHostSystem = 160;
}