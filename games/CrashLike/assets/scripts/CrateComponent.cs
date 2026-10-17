using ECS;

namespace CrashLike.assets.scripts;

[SerializableComponent]
public class CrateComponent : IGameComponent
{
    public const int Basic = 0;
    public const int Question = 1;
    public const int Bounce = 2;
    public const int Tnt = 3;

    public int CrateType { get; set; }
    public int LootWumpa { get; set; }
    public bool BonusLife { get; set; }
    public bool Broken { get; set; }
    public bool Lit { get; set; }
    public float FuseTimer { get; set; }
    public float HomeX { get; set; }
    public float HomeY { get; set; }
    public float HomeZ { get; set; }
    public float HalfWidth { get; set; } = 0.5f;
    public float HalfHeight { get; set; } = 0.5f;
    public float HalfDepth { get; set; } = 0.5f;

    public IComponent Clone() => new CrateComponent
    {
        CrateType = CrateType,
        LootWumpa = LootWumpa,
        BonusLife = BonusLife,
        Broken = Broken,
        Lit = Lit,
        FuseTimer = FuseTimer,
        HomeX = HomeX,
        HomeY = HomeY,
        HomeZ = HomeZ,
        HalfWidth = HalfWidth,
        HalfHeight = HalfHeight,
        HalfDepth = HalfDepth
    };
}
