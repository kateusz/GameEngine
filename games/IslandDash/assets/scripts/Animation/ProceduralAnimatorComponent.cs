using ECS;

namespace IslandDash;

/// <summary>World-space pose of a visual for one frame, relative to its gameplay owner.</summary>
public struct Pose
{
    /// <summary>Translation added to the owner's position (world space).</summary>
    public Vector3 Offset;
    /// <summary>Full world orientation of the model.</summary>
    public Quaternion Rotation;
    /// <summary>Multiplies the model's rest scale; scaling happens around the model pivot (the feet for characters).</summary>
    public Vector3 Scale;

    public static Pose At(Quaternion rotation) => new() { Rotation = rotation, Scale = Vector3.One };
}

/// <summary>One-shot animation cues fired by gameplay; drivers consume them on their next evaluation.</summary>
[Flags]
public enum AnimationEvent
{
    None = 0,
    Jump = 1 << 0,
    AirJump = 1 << 1,
    Land = 1 << 2,
    Spin = 1 << 3,
    Die = 1 << 4,
    Respawn = 1 << 5,
    /// <summary>Rise, spin and shrink away (collected crystal, broken crate reward).</summary>
    Pop = 1 << 6,
    Squash = 1 << 7
}

/// <summary>Which <see cref="IAnimationDriver"/> evaluates an animator. See <see cref="AnimationDrivers"/>.</summary>
public enum AnimationDriverKind
{
    Static,
    Player,
    Collectible,
    Enemy,
    Platform,
    Pop
}

/// <summary>
/// Animation front-end for one gameplay entity. Gameplay code only writes the parameters and fires events below;
/// an <see cref="IAnimationDriver"/> turns them into a <see cref="Pose"/> that
/// <see cref="ProceduralAnimationSystem"/> applies to the separate visual entity (<see cref="VisualEntityId"/>).
/// The gameplay transform and colliders are never touched by animation.
/// To animate by hand or with a skeletal system later, register a different driver for the same
/// <see cref="AnimationDriverKind"/>; gameplay code does not change.
/// </summary>
public sealed class ProceduralAnimatorComponent : IGameComponent
{
    public int VisualEntityId { get; set; } = -1;
    public AnimationDriverKind Driver { get; set; }

    // Rest pose: model normalization from the AssetManifest (scale, pivot) and the piece's placement yaw.
    public Vector3 RestScale { get; set; } = Vector3.One;
    public Vector3 RestOffset { get; set; }
    public float RestYaw { get; set; }

    // ---- Parameters (written by gameplay every frame as needed) ----
    /// <summary>Horizontal speed, 0 = idle, 1 = full run.</summary>
    public float Speed01 { get; set; }
    public bool Grounded { get; set; } = true;
    public float VerticalSpeed { get; set; }
    /// <summary>Yaw the model should turn toward (radians around +Y; 0 faces +Z).</summary>
    public float FacingYaw { get; set; }
    public bool Spinning { get; set; }
    /// <summary>0..1 jitter (falling platform about to drop, crusher about to slam).</summary>
    public float Shake { get; set; }
    /// <summary>Flicker visibility (vanishing-platform warning, respawn invulnerability).</summary>
    public bool Blink { get; set; }
    public bool Hidden { get; set; }
    /// <summary>0..1 strength of the next landing squash.</summary>
    public float LandImpact { get; set; }
    public float SpinSpeed { get; set; } = 2.4f;
    public float BobHeight { get; set; } = 0.12f;

    public AnimationEvent PendingEvents { get; set; }

    public void Fire(AnimationEvent animationEvent) => PendingEvents |= animationEvent;

    public bool Consume(AnimationEvent animationEvent)
    {
        if ((PendingEvents & animationEvent) == 0)
            return false;
        PendingEvents &= ~animationEvent;
        return true;
    }

    // ---- Driver-owned state ----
    public float Time { get; set; }
    /// <summary>Per-instance random phase so identical pieces don't move in lockstep.</summary>
    public float Phase { get; set; }
    public float Yaw { get; set; }
    public float Lean { get; set; }
    public float Roll { get; set; }
    public float Stride { get; set; }
    public float Squash { get; set; }
    public float SquashVelocity { get; set; }
    public float SpinAngle { get; set; }
    /// <summary>Running one-shot sequence (Die / Respawn / Pop / Squash) and its elapsed time.</summary>
    public AnimationEvent Sequence { get; set; }
    public float SequenceTime { get; set; }

    public void StartSequence(AnimationEvent sequence)
    {
        Sequence = sequence;
        SequenceTime = 0f;
    }

    public IComponent Clone() => (ProceduralAnimatorComponent)MemberwiseClone();
}
