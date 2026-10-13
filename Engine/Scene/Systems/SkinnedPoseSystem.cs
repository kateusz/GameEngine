using ECS;
using ECS.Systems;
using Engine.Renderer.Models;

namespace Engine.Scene.Systems;

internal sealed class SkinnedPoseSystem(Context context, IModelFactory models) : ISystem
{
    public int Priority => 145;

    public void OnUpdate(TimeSpan deltaTime) => SkinnedPose.Advance(context, models, deltaTime);
}
