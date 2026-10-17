using ECS;

namespace IslandDash;

/// <summary>Default third-person camera rig. Level camera zones override yaw/pitch/distance/height per section.</summary>
[SerializableComponent]
public class CameraTuningComponent : IGameComponent
{
    public float Distance { get; set; } = 7.5f;
    /// <summary>Height of the look-at point above the player's feet.</summary>
    public float FocusHeight { get; set; } = 1.4f;
    /// <summary>Radians; negative looks down.</summary>
    public float Pitch { get; set; } = -0.36f;
    /// <summary>Radians; 0 looks down the track (-Z).</summary>
    public float Yaw { get; set; }
    public float LookAhead { get; set; } = 1.2f;

    public float FollowSharpness { get; set; } = 7f;
    /// <summary>Slower vertical follow so jumps don't bounce the view.</summary>
    public float VerticalSharpness { get; set; } = 2.5f;
    public float ZoneBlendSharpness { get; set; } = 2.5f;
    public float ArmReturnSharpness { get; set; } = 3f;

    /// <summary>Q/E orbit speed (rad/s) and right-mouse-drag sensitivity (rad/pixel); disabled in fixed zones.</summary>
    public float OrbitSpeed { get; set; } = 1.8f;
    public float MouseOrbitSensitivity { get; set; } = 0.005f;

    public float CollisionMargin { get; set; } = 0.35f;
    public float MinDistance { get; set; } = 1.6f;

    public IComponent Clone() => (CameraTuningComponent)MemberwiseClone();
}
