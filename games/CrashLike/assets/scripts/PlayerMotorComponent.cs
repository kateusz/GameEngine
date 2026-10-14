using ECS;

namespace CrashLike.assets.scripts;

/// <summary>
/// World-space body size and facing for the player. Half extents match the physics box.
/// </summary>
[SerializableComponent]
public class PlayerMotorComponent : IGameComponent
{
    public float HalfWidth { get; set; } = 0.34f;
    public float HalfHeight { get; set; } = 0.59f;
    public int Facing { get; set; } = 1;

    public IComponent Clone() => new PlayerMotorComponent
    {
        HalfWidth = HalfWidth,
        HalfHeight = HalfHeight,
        Facing = Facing
    };
}
