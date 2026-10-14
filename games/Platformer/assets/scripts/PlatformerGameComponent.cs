using ECS;

namespace Platformer.assets.scripts;

/// <summary>
/// Serializable state for a side-view platformer (run, jump, stomps, coins, flag).
/// The level is a grid of characters. Row 0 is the top of the map.
/// </summary>
/// <remarks>
/// <para><c>.</c> air, <c>#</c> ground, <c>B</c> crate, <c>?</c> item block, <c>U</c> used block,
/// <c>=</c> platform, <c>C</c> coin, <c>F</c> flag, <c>^</c> spikes, <c>P</c> player spawn, <c>G</c> enemy spawn.</para>
/// </remarks>
[SerializableComponent]
public class PlatformerGameComponent : IGameComponent
{
    public const int Ready = 0;
    public const int Playing = 1;
    public const int Dead = 2;
    public const int Won = 3;

    public const int MaxEnemies = 6;
    public const int PoolColumns = 34;
    public const int PoolRows = 18;

    public int Phase { get; set; } = Ready;
    public float PlayerX { get; set; }
    public float PlayerY { get; set; }
    public float VelX { get; set; }
    public float VelY { get; set; }
    public int Facing { get; set; } = 1;
    public bool Grounded { get; set; }
    public int Coins { get; set; }
    public int Score { get; set; }
    public int Lives { get; set; } = 3;
    public float Invuln { get; set; }
    public float SpawnX { get; set; }
    public float SpawnY { get; set; }
    public float CamX { get; set; }
    public float CamY { get; set; }

    public string[] Template { get; set; } = DefaultLevel();
    public string[] Rows { get; set; } = DefaultLevel();

    public int EnemyCount { get; set; }
    public float[] EnemyX { get; set; } = new float[MaxEnemies];
    public float[] EnemyY { get; set; } = new float[MaxEnemies];
    public float[] EnemyVelX { get; set; } = new float[MaxEnemies];
    public float[] EnemyVelY { get; set; } = new float[MaxEnemies];
    public bool[] EnemyAlive { get; set; } = new bool[MaxEnemies];

    public float TileSize { get; set; } = 1f;
    public float Gravity { get; set; } = 42f;
    public float JumpSpeed { get; set; } = 16.5f;
    public float JumpCut { get; set; } = 6.5f;
    public float RunSpeed { get; set; } = 7.5f;
    public float MoveAccel { get; set; } = 60f;
    public float GroundFriction { get; set; } = 80f;
    public float MaxFall { get; set; } = 16f;
    public float PlayerHalfW { get; set; } = 0.28f;
    public float PlayerHalfH { get; set; } = 0.42f;
    public float EnemyHalfW { get; set; } = 0.32f;
    public float EnemyHalfH { get; set; } = 0.32f;
    public float EnemySpeed { get; set; } = 2.4f;
    public float StompBounce { get; set; } = 10f;
    public float InvulnTime { get; set; } = 1.25f;
    public float OrthoSize { get; set; } = 6f;
    public float Aspect { get; set; } = 16f / 9f;
    public int CoinScore { get; set; } = 100;
    public int StompScore { get; set; } = 100;

    public int Height => Rows.Length;

    public int Width => Rows.Length == 0 ? 0 : Rows[0].Length;

    public static string[] DefaultLevel() =>
    [
        "....................................................................",
        "....................................................................",
        "....................................................................",
        "....................................................................",
        "....................................................................",
        "....................................................................",
        "..................................C.G.C..................G..C.......",
        "................................=========.............=========.....",
        ".....??..............B?.............................................",
        "..................................................................##",
        "...P....C...........C...............C...............C.........F.....",
        "############..##############..################..####################",
        "############..##############..################..####################",
        "############..##############..################^^####################",
    ];

    public IComponent Clone() => new PlatformerGameComponent
    {
        Phase = Phase,
        PlayerX = PlayerX,
        PlayerY = PlayerY,
        VelX = VelX,
        VelY = VelY,
        Facing = Facing,
        Grounded = Grounded,
        Coins = Coins,
        Score = Score,
        Lives = Lives,
        Invuln = Invuln,
        SpawnX = SpawnX,
        SpawnY = SpawnY,
        CamX = CamX,
        CamY = CamY,
        Template = (string[])Template.Clone(),
        Rows = (string[])Rows.Clone(),
        EnemyCount = EnemyCount,
        EnemyX = (float[])EnemyX.Clone(),
        EnemyY = (float[])EnemyY.Clone(),
        EnemyVelX = (float[])EnemyVelX.Clone(),
        EnemyVelY = (float[])EnemyVelY.Clone(),
        EnemyAlive = (bool[])EnemyAlive.Clone(),
        TileSize = TileSize,
        Gravity = Gravity,
        JumpSpeed = JumpSpeed,
        JumpCut = JumpCut,
        RunSpeed = RunSpeed,
        MoveAccel = MoveAccel,
        GroundFriction = GroundFriction,
        MaxFall = MaxFall,
        PlayerHalfW = PlayerHalfW,
        PlayerHalfH = PlayerHalfH,
        EnemyHalfW = EnemyHalfW,
        EnemyHalfH = EnemyHalfH,
        EnemySpeed = EnemySpeed,
        StompBounce = StompBounce,
        InvulnTime = InvulnTime,
        OrthoSize = OrthoSize,
        Aspect = Aspect,
        CoinScore = CoinScore,
        StompScore = StompScore
    };
}
