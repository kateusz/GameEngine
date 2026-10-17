using ECS;

namespace IslandDash;

/// <summary>Runtime player controller state. Parameters live in <see cref="PlayerTuningComponent"/>.</summary>
public sealed class PlayerComponent : IGameComponent
{
    public Vector3 Velocity { get; set; }
    public bool Grounded { get; set; }
    public int GroundEntityId { get; set; } = -1;
    public float TimeSinceGrounded { get; set; }
    public float TimeSinceJumpPressed { get; set; } = float.MaxValue;
    public bool JumpConsumed { get; set; }
    public bool JumpCutApplied { get; set; }
    public int AirJumpsUsed { get; set; }

    public float SpinTime { get; set; }
    public float SpinCooldownTime { get; set; }
    public bool IsSpinning => SpinTime > 0f;

    public float InvulnerableTime { get; set; }

    public IComponent Clone() => (PlayerComponent)MemberwiseClone();
}
