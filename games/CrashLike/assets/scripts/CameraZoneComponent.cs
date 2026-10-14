using ECS;

namespace CrashLike.assets.scripts;

/// <summary>
/// While the player X is inside [MinX, MaxX], the camera and movement use this mode.
/// Mode 1 is the into-the-screen corridor: the player auto-runs forward.
/// </summary>
[SerializableComponent]
public class CameraZoneComponent : IGameComponent
{
    public int Mode { get; set; }
    public float MinX { get; set; }
    public float MaxX { get; set; }
    public float LookAhead { get; set; } = 3.1f;
    public float OrthoSize { get; set; } = 4.15f;

    public IComponent Clone() => new CameraZoneComponent
    {
        Mode = Mode,
        MinX = MinX,
        MaxX = MaxX,
        LookAhead = LookAhead,
        OrthoSize = OrthoSize
    };
}
