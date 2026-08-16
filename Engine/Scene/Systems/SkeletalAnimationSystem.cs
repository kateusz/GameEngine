using ECS;
using ECS.Systems;
using Engine.Renderer;
using Engine.Renderer.Models;
using Engine.Scene.Skeletal;

namespace Engine.Scene.Systems;

internal sealed class SkeletalAnimationSystem(IContext context, IModelFactory modelFactory) : ISystem
{
    public int Priority => SystemPriorities.SkeletalAnimationSystem;

    public void OnInit() { }

    public void OnUpdate(TimeSpan deltaTime) =>
        SkeletalPlaybackUpdater.Tick(context, deltaTime, modelFactory);

    public void OnShutdown() { }
}
