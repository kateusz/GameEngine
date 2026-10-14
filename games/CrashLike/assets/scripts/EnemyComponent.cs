using ECS;

namespace CrashLike.assets.scripts;

[SerializableComponent]
public class EnemyComponent : IGameComponent
{
    public const int Patrol = 0;
    public const int Charger = 1;

    public int Kind { get; set; }
    public float HomeX { get; set; }
    public float HomeY { get; set; }
    public float MinX { get; set; }
    public float MaxX { get; set; }
    public float Speed { get; set; } = 1.8f;
    public int Direction { get; set; } = 1;
    public bool Alive { get; set; } = true;
    public float HalfWidth { get; set; } = 0.3f;
    public float HalfHeight { get; set; } = 0.2f;
    public float ChargeRange { get; set; } = 4f;
    public string TexturePath { get; set; } = "textures/enemies/slime.png";

    public IComponent Clone() => new EnemyComponent
    {
        Kind = Kind,
        HomeX = HomeX,
        HomeY = HomeY,
        MinX = MinX,
        MaxX = MaxX,
        Speed = Speed,
        Direction = Direction,
        Alive = Alive,
        HalfWidth = HalfWidth,
        HalfHeight = HalfHeight,
        ChargeRange = ChargeRange,
        TexturePath = TexturePath
    };
}
