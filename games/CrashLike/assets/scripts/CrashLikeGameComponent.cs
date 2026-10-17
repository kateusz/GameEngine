using ECS;

namespace CrashLike.assets.scripts;

/// <summary>
/// Whole-level state for the 3D platformer. Lives on the "Game" entity.
/// Tunables are editable in the inspector; runtime fields reset when the level restarts.
/// </summary>
[SerializableComponent]
public class CrashLikeGameComponent : IGameComponent
{
    public const int Playing = 0;
    public const int GameOver = 1;
    public const int Complete = 2;

    public const int Chase = 0;
    public const int Side = 1;

    public int Phase { get; set; } = Playing;
    public int Lives { get; set; } = 3;
    public int StartingLives { get; set; } = 3;
    public int MaxLives { get; set; } = 5;
    public int Wumpa { get; set; }
    public int WumpaPerLife { get; set; } = 10;
    public int Masks { get; set; }

    public float SpinTimer { get; set; }
    public float InvulnTimer { get; set; }
    public float CoyoteTimer { get; set; }
    public float JumpBuffer { get; set; }
    public bool JumpHeld { get; set; }
    public int CameraMode { get; set; } = Chase;

    public float RespawnX { get; set; }
    public float RespawnY { get; set; } = 1.2f;
    public float RespawnZ { get; set; } = 2f;
    public float StartX { get; set; }
    public float StartY { get; set; } = 1.2f;
    public float StartZ { get; set; } = 2f;
    public bool TeleportPending { get; set; }
    public bool HintVisible { get; set; } = true;

    public float DeathY { get; set; } = -2.4f;
    public float LevelMinX { get; set; }
    public float LevelMaxX { get; set; } = 94f;
    public float CameraY { get; set; } = 2.2f;
    public float SideLookAhead { get; set; } = 1.7f;
    public float DepthLookAhead { get; set; } = 3.1f;
    public float SideOrtho { get; set; } = 4.6f;
    public float DepthOrtho { get; set; } = 4.15f;
    public float CameraSharpness { get; set; } = 8f;

    public float MoveSpeed { get; set; } = 7f;
    public float StrafeSpeed { get; set; } = 5.5f;
    public float Gravity { get; set; } = 18f;
    public float GroundAccel { get; set; } = 70f;
    public float AirAccel { get; set; } = 28f;
    public float JumpSpeed { get; set; } = 10.6f;
    public float JumpCut { get; set; } = 0.45f;
    public float SpinTime { get; set; } = 0.38f;
    public float InvulnTime { get; set; } = 1.15f;
    public float CoyoteTime { get; set; } = 0.12f;
    public float JumpBufferTime { get; set; } = 0.12f;
    public float StompBounce { get; set; } = 8.5f;
    public float BounceSpeed { get; set; } = 13.5f;

    public IComponent Clone() => new CrashLikeGameComponent
    {
        Phase = Phase,
        Lives = Lives,
        StartingLives = StartingLives,
        MaxLives = MaxLives,
        Wumpa = Wumpa,
        WumpaPerLife = WumpaPerLife,
        Masks = Masks,
        SpinTimer = SpinTimer,
        InvulnTimer = InvulnTimer,
        CoyoteTimer = CoyoteTimer,
        JumpBuffer = JumpBuffer,
        JumpHeld = JumpHeld,
        CameraMode = CameraMode,
        RespawnX = RespawnX,
        RespawnY = RespawnY,
        RespawnZ = RespawnZ,
        StartX = StartX,
        StartY = StartY,
        StartZ = StartZ,
        TeleportPending = TeleportPending,
        HintVisible = HintVisible,
        DeathY = DeathY,
        LevelMinX = LevelMinX,
        LevelMaxX = LevelMaxX,
        CameraY = CameraY,
        SideLookAhead = SideLookAhead,
        DepthLookAhead = DepthLookAhead,
        SideOrtho = SideOrtho,
        DepthOrtho = DepthOrtho,
        CameraSharpness = CameraSharpness,
        MoveSpeed = MoveSpeed,
        StrafeSpeed = StrafeSpeed,
        Gravity = Gravity,
        GroundAccel = GroundAccel,
        AirAccel = AirAccel,
        JumpSpeed = JumpSpeed,
        JumpCut = JumpCut,
        SpinTime = SpinTime,
        InvulnTime = InvulnTime,
        CoyoteTime = CoyoteTime,
        JumpBufferTime = JumpBufferTime,
        StompBounce = StompBounce,
        BounceSpeed = BounceSpeed
    };
}
