using ECS;

namespace CrashLike.assets.scripts;

/// <summary>Axis-aligned blocker. The mesh scale is visual; these halves are the collision box.</summary>
[SerializableComponent]
public class SolidComponent : IGameComponent
{
    public float HalfX { get; set; } = 0.5f;
    public float HalfY { get; set; } = 0.5f;
    public float HalfZ { get; set; } = 0.5f;

    public IComponent Clone() => new SolidComponent
    {
        HalfX = HalfX,
        HalfY = HalfY,
        HalfZ = HalfZ
    };
}
