using ECS;

namespace IslandDash;

// Runtime components for level kit pieces. Built from the level file each run, so none of them are scene-serialized.

public sealed class CollectibleComponent : IGameComponent
{
    public float Radius { get; set; } = 0.6f;
    public bool Collected { get; set; }
    public IComponent Clone() => (CollectibleComponent)MemberwiseClone();
}

public enum CrateKind
{
    Normal,
    ExtraLife,
    Checkpoint
}

public sealed class CrateComponent : IGameComponent
{
    public CrateKind Kind { get; set; }
    public int Crystals { get; set; } = 2;
    public bool Broken { get; set; }
    /// <summary>Model swapped onto the crate's visual when it breaks (pops out and fades).</summary>
    public ModelEntry? Reward { get; set; }
    public IComponent Clone() => (CrateComponent)MemberwiseClone();
}

/// <summary>Touching it kills the player while <see cref="Active"/>.</summary>
public sealed class HazardComponent : IGameComponent
{
    public bool Active { get; set; } = true;
    public IComponent Clone() => (HazardComponent)MemberwiseClone();
}

public enum EnemyBehavior
{
    Patrol,
    Chase
}

public sealed class EnemyComponent : IGameComponent
{
    public EnemyBehavior Behavior { get; set; }
    public Vector3 Home { get; set; }
    /// <summary>Patrol waypoints in world space (ping-pong).</summary>
    public List<Vector3> Waypoints { get; set; } = [];
    public int TargetWaypoint { get; set; }
    public float Speed { get; set; } = 2f;
    public float ChaseSpeed { get; set; } = 4f;
    public float ChaseRadius { get; set; } = 6f;
    /// <summary>Max distance a chaser strays from home.</summary>
    public float Leash { get; set; } = 7f;
    public bool Alive { get; set; } = true;
    public float DefeatTime { get; set; }
    public Vector3 KnockVelocity { get; set; }
    public IComponent Clone() => (EnemyComponent)MemberwiseClone();
}

/// <summary>Kinematic platform following a sine offset or ping-pong waypoints from its origin.</summary>
public sealed class MovingPlatformComponent : IGameComponent
{
    public Vector3 Origin { get; set; }
    public Vector3 Amplitude { get; set; }
    public float Period { get; set; } = 4f;
    public float Phase { get; set; }
    public List<Vector3> Waypoints { get; set; } = [];
    public float Speed { get; set; } = 2f;
    public float Time { get; set; }
    public IComponent Clone() => (MovingPlatformComponent)MemberwiseClone();
}

/// <summary>Solid for <see cref="OnTime"/>, gone for <see cref="OffTime"/>, repeating.</summary>
public sealed class VanishingPlatformComponent : IGameComponent
{
    public float OnTime { get; set; } = 2.5f;
    public float OffTime { get; set; } = 1.5f;
    public float Phase { get; set; }
    public float WarningTime { get; set; } = 0.7f;
    public float Time { get; set; }
    public IComponent Clone() => (VanishingPlatformComponent)MemberwiseClone();
}

public enum FallerState
{
    Idle,
    Shaking,
    Falling,
    Gone
}

/// <summary>Shakes once stood on, then drops; reappears after <see cref="RespawnTime"/>.</summary>
public sealed class FallingPlatformComponent : IGameComponent
{
    public Vector3 Origin { get; set; }
    public FallerState State { get; set; }
    public float ShakeTime { get; set; } = 0.55f;
    public float RespawnTime { get; set; } = 3.5f;
    public float Timer { get; set; }
    public float FallSpeed { get; set; }
    public IComponent Clone() => (FallingPlatformComponent)MemberwiseClone();
}

/// <summary>Spiked block that hovers, shakes, slams down, waits and rises again.</summary>
public sealed class CrusherComponent : IGameComponent
{
    public Vector3 Bottom { get; set; }
    public float Height { get; set; } = 3f;
    public float Period { get; set; } = 3f;
    public float Phase { get; set; }
    public float Time { get; set; }
    public IComponent Clone() => (CrusherComponent)MemberwiseClone();
}

/// <summary>Spikes that pop out of the floor on a cycle. Static spikes have no cycle component.</summary>
public sealed class TimedSpikesComponent : IGameComponent
{
    public float UpTime { get; set; } = 1.2f;
    public float DownTime { get; set; } = 1.2f;
    public float Phase { get; set; }
    public float Time { get; set; }
    public float FloorY { get; set; }
    /// <summary>How far the spikes are currently retracted below <see cref="FloorY"/>.</summary>
    public float Depth { get; set; }
    public IComponent Clone() => (TimedSpikesComponent)MemberwiseClone();
}

public sealed class GoalComponent : IGameComponent
{
    public bool Reached { get; set; }
    public IComponent Clone() => (GoalComponent)MemberwiseClone();
}

/// <summary>Camera override volume; the rig blends to these settings while the player is inside.</summary>
public sealed class CameraZoneComponent : IGameComponent
{
    public Box Bounds { get; set; }
    public float Yaw { get; set; }
    public float Pitch { get; set; } = -0.36f;
    public float Distance { get; set; } = 7.5f;
    public float FocusHeight { get; set; } = 1.4f;
    /// <summary>Locks the view: manual orbit is disabled and its offset fades out.</summary>
    public bool Fixed { get; set; }
    public IComponent Clone() => (CameraZoneComponent)MemberwiseClone();
}

/// <summary>Runtime state of the third-person camera rig, on the primary camera entity.</summary>
public sealed class CameraRigComponent : IGameComponent
{
    public Vector3 Focus { get; set; }
    public float Yaw { get; set; }
    public float Pitch { get; set; }
    public float Distance { get; set; }
    public float FocusHeight { get; set; }
    public float UserYaw { get; set; }
    public float ArmLength { get; set; }
    public Quaternion Orientation { get; set; } = Quaternion.Identity;
    public IComponent Clone() => (CameraRigComponent)MemberwiseClone();
}
