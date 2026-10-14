using ECS;

namespace CrashLike.assets.scripts;

[SerializableComponent]
public class HazardComponent : IGameComponent
{
    public float HalfWidth { get; set; } = 0.3f;
    public float HalfHeight { get; set; } = 0.2f;

    public IComponent Clone() => new HazardComponent
    {
        HalfWidth = HalfWidth,
        HalfHeight = HalfHeight
    };
}
