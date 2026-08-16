using ECS;
using ECS.Systems;
using Engine.Scene.Skeletal;

namespace Engine.Scene.Systems;

internal sealed class SkeletalAnimationSystem(IContext context) : ISystem
{
    public int Priority => SystemPriorities.SkeletalAnimationSystem;

    public void OnInit() { }

    public void OnUpdate(TimeSpan deltaTime) =>
        SkeletalPlaybackUpdater.Tick(context, deltaTime);

    public void OnShutdown() { }
}
