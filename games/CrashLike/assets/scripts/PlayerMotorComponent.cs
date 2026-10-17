using ECS;

namespace CrashLike.assets.scripts;

/// <summary>
/// Collision box and the velocity the 3D controller integrates by hand.
/// Rigid bodies are not used: the 2D physics world zeroes Z every frame.
/// </summary>
[SerializableComponent]
public class PlayerMotorComponent : IGameComponent
{
    public float HalfWidth { get; set; } = 0.34f;
    public float HalfHeight { get; set; } = 0.7f;
    public float HalfDepth { get; set; } = 0.34f;
    public float VelocityX { get; set; }
    public float VelocityY { get; set; }
    public float VelocityZ { get; set; }
    public bool Grounded { get; set; }

    public IComponent Clone() => new PlayerMotorComponent
    {
        HalfWidth = HalfWidth,
        HalfHeight = HalfHeight,
        HalfDepth = HalfDepth,
        VelocityX = VelocityX,
        VelocityY = VelocityY,
        VelocityZ = VelocityZ,
        Grounded = Grounded
    };
}
