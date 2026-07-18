using System.Numerics;
using Audio;
using ECS;
using ECS.Systems;
using Input;
using SceneComponents;
using SceneComponents.Physics;
using SceneComponents.Rendering;
using Scripting;

namespace ArenaShooter.assets.scripts;

/// <summary>
/// Twin-stick arena shooter (Snake-style single-system game).
/// WASD moves the Dynamic player body; mouse aims and hold-LMB auto-fires a hitscan
/// <see cref="IPhysicsQueries.Raycast"/>. Kinematic enemies chase the player and
/// are pooled: killed/spawned by parking them off-screen and reactivating them.
/// </summary>
[Register(typeof(IGameSystem))]
public class ArenaSystem(
    IContext context,
    IKeyboardInput keyboard,
    IMouseInput mouse,
    ICameraQueries cameraQueries,
    IPhysicsQueries physics,
    IPhysicsContacts contacts,
    IAudioPlayback audioPlayback,
    IAudio audio) : IGameSystem
{
    private static readonly Vector3 Graveyard = new(1000f, 1000f, 0f);

    private static readonly Vector4 PlayerColor = new(1f, 1f, 1f, 1f);
    private static readonly Vector4 PlayerHitColor = new(1f, 0.35f, 0.3f, 1f);
    private static readonly Vector4 AimColor = new(1f, 0.9f, 0.25f, 1f);
    private static readonly Vector4 TracerColor = new(1f, 1f, 0.85f, 0.9f);
    private static readonly Vector4 HeartColor = new(0.32f, 0.9f, 0.42f, 1f);
    private static readonly Vector4 GameOverColor = new(0.82f, 0.12f, 0.12f, 0.92f);
    private static readonly Vector4 Hidden = Vector4.Zero;

    private const float DigitSpacing = 0.34f;
    private const float DigitY = 4.15f;

    private readonly Random _rng = new();
    private Entity _weaponEntity;
    private float _zombieCooldown;

    public int Priority => 115;

    public void OnInit()
    {
        _weaponEntity = context.GetByName("Aim");
    }

    public void OnShutdown() { }

    public void OnUpdate(TimeSpan deltaTime)
    {
        var game = FindGame();
        if (game == null)
            return;

        var player = context.GetByName("Player");
        if (!player.TryGetComponent<TransformComponent>(out var playerTransform))
            return;

        // Clamp dt so a lag spike can't teleport enemies through the player before we can react.
        var dt = (float)deltaTime.TotalSeconds;
        if (dt > 0.05f)
            dt = 0.05f;

        if (_zombieCooldown > 0f)
            _zombieCooldown -= dt;

        var playerPos = new Vector2(playerTransform.Translation.X, playerTransform.Translation.Y);

        SyncWaterContacts(game);

        if (game.Phase == ArenaGameComponent.Dead)
        {
            StopBody(player);
            StopAllEnemies();
            StopWalk(game, player);
            if (keyboard.WasKeyPressed(KeyCodes.R))
                Reset(game, player);
            SyncVisuals(game, player, playerPos);
            return;
        }

        HandleMovement(game, player);

        var worldMouse = cameraQueries.ScreenToWorld2D(mouse.Position);
        if (worldMouse is { } target)
            game.Facing = FacingToward(playerPos, target, game.Facing);

        HandleShooting(game, player, playerPos, worldMouse, dt);
        UpdateEnemies(game, playerPos, dt);
        HandleSpawning(game, dt);

        if (game.TracerTimer > 0f)
            game.TracerTimer -= dt;

        if (game.Health <= 0)
        {
            game.Phase = ArenaGameComponent.Dead;
            StopWalk(game, player);
            Console.WriteLine($"Game over! Final score: {game.Score}. Press R to play again.");
        }

        SyncVisuals(game, player, playerPos);
    }

    private ArenaGameComponent? FindGame()
    {
        foreach (var (_, game) in context.View<ArenaGameComponent>())
            return game;
        return null;
    }

    private void SyncWaterContacts(ArenaGameComponent game)
    {
        foreach (var contact in contacts.DrainContacts())
        {
            if (!contact.IsTrigger || !IsPlayerWaterContact(contact.Self, contact.Other))
                continue;

            game.InWater = contact.IsBegin;
        }
    }

    private static bool IsPlayerWaterContact(Entity a, Entity b) =>
        (a.Name == "Player" && b.Name == "Water") || (a.Name == "Water" && b.Name == "Player");

    private void HandleMovement(ArenaGameComponent game, Entity player)
    {
        var speed = game.InWater ? game.MoveSpeed * 0.5f : game.MoveSpeed;
        var velocity = MoveVelocity(
            keyboard.IsKeyDown(KeyCodes.W), keyboard.IsKeyDown(KeyCodes.S),
            keyboard.IsKeyDown(KeyCodes.A), keyboard.IsKeyDown(KeyCodes.D),
            speed);

        if (player.TryGetComponent<RigidBody2DComponent>(out var body))
            body.Velocity = velocity;

        SyncWalk(game, player, moving: velocity.LengthSquared() > 0f);
    }

    private void SyncWalk(ArenaGameComponent game, Entity player, bool moving)
    {
        if (moving == game.WalkPlaying)
            return;

        if (moving)
            audioPlayback.Play(player);
        else
            audioPlayback.Pause(player);

        game.WalkPlaying = moving;
    }

    private void StopWalk(ArenaGameComponent game, Entity player)
    {
        if (!game.WalkPlaying)
            return;

        audioPlayback.Stop(player);
        game.WalkPlaying = false;
    }

    private void HandleShooting(
        ArenaGameComponent game,
        Entity player,
        Vector2 playerPos,
        Vector2? worldMouse,
        float dt)
    {
        if (game.FireCooldown > 0f)
            game.FireCooldown -= dt;

        // Outside surface / no camera → ScreenToWorld2D is null; don't fire into editor UI.
        if (worldMouse is null || !mouse.IsButtonDown(MouseButtons.Left) || game.FireCooldown > 0f)
            return;

        game.FireCooldown = game.FireInterval;
        audioPlayback.Play(_weaponEntity);

        var dir = game.Facing.LengthSquared() > 0f ? Vector2.Normalize(game.Facing) : new Vector2(1f, 0f);
        var hit = physics.Raycast(playerPos, dir, game.ShootRange, ignoreEntity: player, includeTriggers: false);

        game.TracerStart = playerPos;
        game.TracerEnd = hit is { } h ? h.Point : playerPos + dir * game.ShootRange;
        game.TracerTimer = game.TracerTime;

        if (hit is { } enemyHit &&
            enemyHit.Entity.TryGetComponent<EnemyComponent>(out var enemy) &&
            enemy.Alive)
        {
            KillEnemy(enemyHit.Entity, enemy, playDeathSound: true);
            game.Score++;
            Console.WriteLine($"Enemy down! Score: {game.Score}");
        }
    }

    private void UpdateEnemies(ArenaGameComponent game, Vector2 playerPos, float dt)
    {
        if (game.InvulnTimer > 0f)
            game.InvulnTimer -= dt;

        foreach (var (entity, enemy) in context.View<EnemyComponent>())
        {
            if (!enemy.Alive || !entity.TryGetComponent<TransformComponent>(out var transform))
                continue;

            var pos = new Vector2(transform.Translation.X, transform.Translation.Y);
            var toPlayer = playerPos - pos;
            var distance = toPlayer.Length();

            if (entity.TryGetComponent<RigidBody2DComponent>(out var body))
                body.Velocity = distance > 0.0001f
                    ? Vector2.Normalize(toPlayer) * game.EnemySpeed
                    : Vector2.Zero;

            // ponytail: radius contact check instead of IPhysicsContacts, and enemies are Kinematic
            // (so they ignore walls and can trail the player past one). Upgrade path: Dynamic enemy
            // bodies with runtime spawn/despawn + DrainContacts() if precise collisions are needed.
            if (distance <= game.ContactRadius)
            {
                KillEnemy(entity, enemy, playDeathSound: true);
                if (game.InvulnTimer <= 0f)
                {
                    game.Health--;
                    game.InvulnTimer = game.InvulnTime;
                    Console.WriteLine($"Hit! Health: {game.Health}");
                }
            }
        }
    }

    private const int SpawnPerWave = 4;

    private void HandleSpawning(ArenaGameComponent game, float dt)
    {
        game.SpawnTimer -= dt;
        if (game.SpawnTimer > 0f)
            return;

        game.SpawnTimer = game.SpawnInterval;

        var active = 0;
        foreach (var (_, enemy) in context.View<EnemyComponent>())
        {
            if (enemy.Alive)
                active++;
        }

        for (var i = 0; i < SpawnPerWave && active < game.MaxActiveEnemies; i++)
        {
            Entity? slot = null;
            EnemyComponent? slotComponent = null;
            foreach (var (entity, enemy) in context.View<EnemyComponent>())
            {
                if (enemy.Alive)
                    continue;
                slot = entity;
                slotComponent = enemy;
                break;
            }

            if (slot == null || slotComponent == null)
                break;

            // One per side (top/bottom/left/right) so the wave spreads out.
            var spawn = EdgeSpawnPoint(game, _rng, side: i % 4);
            slotComponent.Alive = true;
            if (slot.TryGetComponent<TransformComponent>(out var transform))
                transform.Translation = new Vector3(spawn.X, spawn.Y, 0f);
            if (slot.TryGetComponent<RigidBody2DComponent>(out var body))
                body.Velocity = Vector2.Zero;
            active++;
        }
    }

    private void KillEnemy(Entity entity, EnemyComponent enemy, bool playDeathSound = false)
    {
        enemy.Alive = false;
        if (entity.TryGetComponent<TransformComponent>(out var transform))
            transform.Translation = Graveyard;
        if (entity.TryGetComponent<RigidBody2DComponent>(out var body))
            body.Velocity = Vector2.Zero;
        
        if (playDeathSound && _zombieCooldown <= 0f && _rng.Next(8) == 0)
        {
            const string zombiePath = "assets/sounds/zombie.wav";
            _zombieCooldown = audio.LoadAudioClip(zombiePath).Duration;
            audio.PlayOneShot(zombiePath);
        }
    }

    private void StopAllEnemies()
    {
        foreach (var (entity, _) in context.View<EnemyComponent>())
        {
            if (entity.TryGetComponent<RigidBody2DComponent>(out var body))
                body.Velocity = Vector2.Zero;
        }
    }

    private static void StopBody(Entity entity)
    {
        if (entity.TryGetComponent<RigidBody2DComponent>(out var body))
            body.Velocity = Vector2.Zero;
    }

    private void Reset(ArenaGameComponent game, Entity player)
    {
        game.Phase = ArenaGameComponent.Playing;
        game.Health = game.MaxHealth;
        game.Score = 0;
        game.Facing = new Vector2(1f, 0f);
        game.FireCooldown = 0f;
        game.SpawnTimer = 0f;
        game.InvulnTimer = 0f;
        game.TracerTimer = 0f;
        game.InWater = false;
        StopWalk(game, player);

        foreach (var (entity, enemy) in context.View<EnemyComponent>())
            KillEnemy(entity, enemy);

        StopBody(player);
        Console.WriteLine("New game! Move with WASD, aim with the mouse, hold LMB to shoot.");
    }

    // --- Visual sync (colored quads; A=0 hides a sprite, per the renderer) ---

    private void SyncVisuals(ArenaGameComponent game, Entity player, Vector2 playerPos)
    {
        var dead = game.Phase == ArenaGameComponent.Dead;

        if (player.TryGetComponent<SpriteRendererComponent>(out var playerSprite))
            playerSprite.Color = dead
                ? Hidden
                : IsFlashing(game.InvulnTimer) ? PlayerHitColor : PlayerColor;

        foreach (var (entity, enemy) in context.View<EnemyComponent>())
        {
            // Enemies use the gucz.png sprite; white shows it untinted, A=0 hides it.
            if (entity.TryGetComponent<SpriteRendererComponent>(out var sprite))
                sprite.Color = enemy.Alive && !dead ? Vector4.One : Hidden;
        }

        SyncAim(game, playerPos, dead);
        SyncTracer(game, dead);
        SyncHearts(game.Health);
        SyncScore(game.Score);
        SetSpriteColor("GameOverBanner", dead ? GameOverColor : Hidden);
    }

    private void SyncAim(ArenaGameComponent game, Vector2 playerPos, bool dead)
    {
        var aim = context.GetByName("Aim");
        var facing = game.Facing.LengthSquared() > 0f ? Vector2.Normalize(game.Facing) : new Vector2(1f, 0f);
        if (aim.TryGetComponent<TransformComponent>(out var transform))
        {
            transform.Translation = new Vector3(
                playerPos.X + facing.X * game.AimOffset,
                playerPos.Y + facing.Y * game.AimOffset,
                0.1f);
            transform.Rotation = transform.Rotation with { Z = MathF.Atan2(facing.Y, facing.X) };
        }

        if (aim.TryGetComponent<SpriteRendererComponent>(out var sprite))
            sprite.Color = dead ? Hidden : AimColor;
    }

    private void SyncTracer(ArenaGameComponent game, bool dead)
    {
        var tracer = context.GetByName("Tracer");
        var show = game.TracerTimer > 0f && !dead;
        if (show && tracer.TryGetComponent<TransformComponent>(out var transform))
        {
            var (center, angle, length) = TracerGeometry(game.TracerStart, game.TracerEnd);
            transform.Translation = new Vector3(center.X, center.Y, 0.1f);
            transform.Rotation = transform.Rotation with { Z = angle };
            transform.Scale = new Vector3(length, 0.06f, 1f);
        }

        if (tracer.TryGetComponent<SpriteRendererComponent>(out var sprite))
            sprite.Color = show ? TracerColor : Hidden;
    }

    private void SyncHearts(int health)
    {
        for (var i = 0; i < health; i++)
        {
            var heart = context.GetByName($"Heart{i}");
            if (heart.TryGetComponent<SpriteRendererComponent>(out var sprite))
                sprite.Color = i < health ? HeartColor : Hidden;
        }
    }

    private void SyncScore(int score)
    {
        var text = score.ToString();

        var capacity = 0;
        foreach (var _ in context.View<ScoreDigitComponent>())
            capacity++;
        if (capacity > 0 && text.Length > capacity)
            text = text[^capacity..];

        var digits = text.Length;
        foreach (var (entity, digit) in context.View<ScoreDigitComponent>())
        {
            if (!entity.TryGetComponent<TransformComponent>(out var transform))
                continue;
            if (!entity.TryGetComponent<SpriteRendererComponent>(out var sprite))
                continue;

            if (digit.Place < digits)
            {
                var x = (digit.Place - (digits - 1) * 0.5f) * DigitSpacing;
                var p = transform.Translation;
                transform.Translation = new Vector3(x, DigitY, p.Z);
                transform.Scale = new Vector3(0.24f, 0.36f, 1f);
                sprite.TexturePath = $"textures/UI/Numbers/{text[digit.Place]}.png";
                sprite.Color = Vector4.One;
            }
            else
            {
                sprite.TexturePath = null;
                sprite.Color = Hidden;
                transform.Scale = Vector3.Zero;
            }
        }
    }

    private void SetSpriteColor(string entityName, Vector4 color)
    {
        var entity = context.GetByName(entityName);
        if (entity.TryGetComponent<SpriteRendererComponent>(out var sprite))
            sprite.Color = color;
    }

    // --- Pure helpers (static, engine-free, unit-tested in ArenaShooterSystemTests) ---

    /// <summary>WASD → world-space velocity, normalized so diagonals aren't faster.</summary>
    public static Vector2 MoveVelocity(bool up, bool down, bool left, bool right, float speed)
    {
        var dir = Vector2.Zero;
        if (up) dir.Y += 1f;
        if (down) dir.Y -= 1f;
        if (right) dir.X += 1f;
        if (left) dir.X -= 1f;
        return dir == Vector2.Zero ? Vector2.Zero : Vector2.Normalize(dir) * speed;
    }

    /// <summary>Unit facing from player toward a world mouse point; keeps current if coincident.</summary>
    public static Vector2 FacingToward(Vector2 playerPos, Vector2 worldMouse, Vector2 current)
    {
        var delta = worldMouse - playerPos;
        return delta.LengthSquared() < 1e-8f ? current : Vector2.Normalize(delta);
    }

    /// <summary>Random point on the play-area rectangle's perimeter (enemy entry points).</summary>
    public static Vector2 EdgeSpawnPoint(ArenaGameComponent game, Random rng, int? side = null)
    {
        var x = game.MinX + (float)rng.NextDouble() * (game.MaxX - game.MinX);
        var y = game.MinY + (float)rng.NextDouble() * (game.MaxY - game.MinY);
        return (side ?? rng.Next(4)) switch
        {
            0 => new Vector2(x, game.MaxY),
            1 => new Vector2(x, game.MinY),
            2 => new Vector2(game.MinX, y),
            _ => new Vector2(game.MaxX, y)
        };
    }

    /// <summary>Midpoint, rotation and length of a shot segment, for the tracer quad.</summary>
    public static (Vector2 Center, float Angle, float Length) TracerGeometry(Vector2 start, Vector2 end)
    {
        var delta = end - start;
        return ((start + end) * 0.5f, MathF.Atan2(delta.Y, delta.X), delta.Length());
    }

    // Blink ~5x/sec while the player is briefly invulnerable after a hit.
    private static bool IsFlashing(float invulnTimer) =>
        invulnTimer > 0f && (int)(invulnTimer * 10f) % 2 == 0;
}
