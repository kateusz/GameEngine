using Engine.Scene.Systems;

namespace Engine.Scene;

internal sealed class RenderingSystemsGroup(
    SpriteRenderingSystem spriteRenderingSystem,
    SubTextureRenderingSystem subTextureRenderingSystem,
    LightingSystem lightingSystem,
    ModelRenderingSystem modelRenderingSystem,
    PhysicsDebugRenderSystem physicsDebugRenderSystem)
{
    public SpriteRenderingSystem SpriteRenderingSystem => spriteRenderingSystem;
    public SubTextureRenderingSystem SubTextureRenderingSystem => subTextureRenderingSystem;
    public LightingSystem LightingSystem => lightingSystem;
    public ModelRenderingSystem ModelRenderingSystem => modelRenderingSystem;
    public PhysicsDebugRenderSystem PhysicsDebugRenderSystem => physicsDebugRenderSystem;
}
