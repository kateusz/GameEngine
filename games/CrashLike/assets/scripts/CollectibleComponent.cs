using ECS;

namespace CrashLike.assets.scripts;

[SerializableComponent]
public class CollectibleComponent : IGameComponent
{
    public const int Wumpa = 0;
    public const int Life = 1;
    public const int Mask = 2;
    public const int Goal = 3;

    public int PickupType { get; set; }
    public int Value { get; set; } = 1;
    public bool Taken { get; set; }
    public float HomeX { get; set; }
    public float HomeY { get; set; }
    public float HomeZ { get; set; }
    public float Radius { get; set; } = 0.45f;

    public IComponent Clone() => new CollectibleComponent
    {
        PickupType = PickupType,
        Value = Value,
        Taken = Taken,
        HomeX = HomeX,
        HomeY = HomeY,
        HomeZ = HomeZ,
        Radius = Radius
    };
}
