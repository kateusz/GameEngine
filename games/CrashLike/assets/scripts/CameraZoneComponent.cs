using ECS;

namespace CrashLike.assets.scripts;

/// <summary>
/// While the player Z is inside [MinZ, MaxZ], the camera uses this mode.
/// 0 = chase from behind, 1 = side view. Movement stays on X and Z.
/// </summary>
[SerializableComponent]
public class CameraZoneComponent : IGameComponent
{
    public int Mode { get; set; }
    public float MinX { get; set; }
    public float MaxX { get; set; }
    public float MinZ { get; set; }
    public float MaxZ { get; set; }
    public float LookAhead { get; set; } = 3.1f;
    public float OrthoSize { get; set; } = 4.15f;

    public IComponent Clone() => new CameraZoneComponent
    {
        Mode = Mode,
        MinX = MinX,
        MaxX = MaxX,
        MinZ = MinZ,
        MaxZ = MaxZ,
        LookAhead = LookAhead,
        OrthoSize = OrthoSize
    };
}
