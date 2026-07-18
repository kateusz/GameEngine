using System.Numerics;
using ECS;

namespace ArenaShooter.assets.scripts;

/// <summary>
/// Whole-game state for the arena shooter: player health/score, aim, timers and the
/// arena bounds used for enemy spawning. Lives on a single "Game" entity (Snake pattern).
/// </summary>
[SerializableComponent]
public class ArenaGameComponent : IGameComponent
{
    public const int Playing = 0;
    public const int Dead = 1;

    // --- Runtime state ---
    public int Phase { get; set; } = Playing;
    public int Health { get; set; } = 3;
    public int Score { get; set; }

    /// <summary>Unit vector the player is currently shooting toward (last arrow held).</summary>
    public Vector2 Facing { get; set; } = new(1f, 0f);

    public float FireCooldown { get; set; }
    public float SpawnTimer { get; set; }
    public float InvulnTimer { get; set; }

    // Last shot's ray, so the tracer visual can be drawn for TracerTime seconds.
    public float TracerTimer { get; set; }
    public Vector2 TracerStart { get; set; }
    public Vector2 TracerEnd { get; set; }

    // --- Tunables (editable in the inspector) ---
    public int MaxHealth { get; set; } = 3;
    public float MoveSpeed { get; set; } = 5f;
    public float EnemySpeed { get; set; } = 3.3f;
    public float FireInterval { get; set; } = 0.15f;
    public float SpawnInterval { get; set; } = 1.1f;
    public int MaxActiveEnemies { get; set; } = 6;
    public float ShootRange { get; set; } = 20f;
    public float ContactRadius { get; set; } = 0.62f;
    public float InvulnTime { get; set; } = 0.8f;
    public float TracerTime { get; set; } = 0.05f;
    public float AimOffset { get; set; } = 0.55f;

    // Play-area rectangle (just inside the walls) for edge spawns / clamping.
    public float MinX { get; set; } = -7.6f;
    public float MaxX { get; set; } = 7.6f;
    public float MinY { get; set; } = -4.1f;
    public float MaxY { get; set; } = 4.1f;

    public IComponent Clone() => new ArenaGameComponent
    {
        Phase = Phase,
        Health = Health,
        Score = Score,
        Facing = Facing,
        FireCooldown = FireCooldown,
        SpawnTimer = SpawnTimer,
        InvulnTimer = InvulnTimer,
        TracerTimer = TracerTimer,
        TracerStart = TracerStart,
        TracerEnd = TracerEnd,
        MaxHealth = MaxHealth,
        MoveSpeed = MoveSpeed,
        EnemySpeed = EnemySpeed,
        FireInterval = FireInterval,
        SpawnInterval = SpawnInterval,
        MaxActiveEnemies = MaxActiveEnemies,
        ShootRange = ShootRange,
        ContactRadius = ContactRadius,
        InvulnTime = InvulnTime,
        TracerTime = TracerTime,
        AimOffset = AimOffset,
        MinX = MinX,
        MaxX = MaxX,
        MinY = MinY,
        MaxY = MaxY
    };
}
