using ECS;

namespace IslandDash;

/// <summary>Every player movement parameter in one place. Lives on the scene's "Game" entity, editable in the inspector.</summary>
[SerializableComponent]
public class PlayerTuningComponent : IGameComponent
{
    // Collider (meters, half extents of the body box; the box bottom sits at the player's feet)
    public Vector3 BodyHalfExtents { get; set; } = new(0.32f, 0.6f, 0.32f);

    // Running
    public float MaxRunSpeed { get; set; } = 7.5f;
    public float GroundAcceleration { get; set; } = 55f;
    public float GroundDeceleration { get; set; } = 70f;
    public float AirAcceleration { get; set; } = 32f;
    public float AirDeceleration { get; set; } = 12f;

    // Gravity
    public float Gravity { get; set; } = 32f;
    public float FallGravityMultiplier { get; set; } = 1.55f;
    public float MaxFallSpeed { get; set; } = 28f;

    // Jumping
    public float JumpSpeed { get; set; } = 12f;
    /// <summary>Upward speed is multiplied by this once when the jump button is released early (variable jump height).</summary>
    public float JumpCutMultiplier { get; set; } = 0.45f;
    public float CoyoteTime { get; set; } = 0.12f;
    public float JumpBufferTime { get; set; } = 0.13f;
    /// <summary>0 disables the double jump.</summary>
    public int MaxAirJumps { get; set; } = 1;
    public float AirJumpSpeed { get; set; } = 10.5f;

    // Spin attack
    public float SpinDuration { get; set; } = 0.5f;
    public float SpinCooldown { get; set; } = 0.25f;
    public float SpinRadius { get; set; } = 1.35f;
    /// <summary>Gravity scale while spinning and falling, gives the spin a little hang time.</summary>
    public float SpinFallGravityScale { get; set; } = 0.55f;

    // Bounces off enemies and crates
    public float StompBounceSpeed { get; set; } = 9.5f;
    public float StompBounceHeldSpeed { get; set; } = 13f;
    /// <summary>Minimum downward speed for a landing to break a crate.</summary>
    public float CrateStompMinSpeed { get; set; } = 3f;

    // Death / respawn
    public float DeathDuration { get; set; } = 1.6f;
    public float RespawnInvulnerability { get; set; } = 1.5f;
    public float HardLandingSpeed { get; set; } = 12f;

    public IComponent Clone() => (PlayerTuningComponent)MemberwiseClone();
}
