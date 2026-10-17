using ECS;

namespace CrashLike.assets.scripts;

/// <summary>
/// Visual piece glued to the player. Offset is in the player's local space.
/// </summary>
[SerializableComponent]
public class RiderComponent : IGameComponent
{
    public float LocalX { get; set; }
    public float LocalY { get; set; }
    public float LocalZ { get; set; }

    public IComponent Clone() => new RiderComponent
    {
        LocalX = LocalX,
        LocalY = LocalY,
        LocalZ = LocalZ
    };
}
