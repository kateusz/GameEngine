using ECS;
using ECS.Systems;
using Engine.Renderer;
using Serilog;

namespace Engine.Scene.Systems;

internal sealed class SkeletalAnimationSystem(
    IContext context,
    ISkeletonFactory skeletonFactory,
    IAnim3dFactory anim3dFactory) : ISystem
{
    private static readonly ILogger Logger = Log.ForContext<SkeletalAnimationSystem>();

    public int Priority => SystemPriorities.SkeletalAnimationSystem;

    public void OnInit() =>
        Logger.Debug("SkeletalAnimationSystem initialized with priority {Priority}", Priority);

    public void OnUpdate(TimeSpan deltaTime) =>
        SkeletalPlaybackUpdater.Update(context, skeletonFactory, anim3dFactory, deltaTime);

    public void OnShutdown() { }
}
