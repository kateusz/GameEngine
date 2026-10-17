using System.Numerics;
using Audio;
using ECS;
using ECS.Systems;
using Input;
using Math;
using SceneComponents;
using SceneComponents.Camera;
using SceneComponents.Rendering;
using Scripting;

namespace CrashLike.assets.scripts;

/// <summary>
/// 3D platformer homage. The player runs along +Z into the screen, strafes on X, and jumps on Y.
/// A/D or arrows strafe, Space/W/Up jump, J/Z/Left Shift spin, R restarts.
/// Chase camera sits behind; a side-view zone swings the camera without changing the axes.
/// </summary>
[Register(typeof(IGameSystem))]
public class CrashLikeSystem(Context context, IKeyboardInput keyboard, IAudio audio) : IGameSystem
{
    private const string SfxJump = "assets/sounds/jump.ogg";
    private const string SfxSpin = "assets/sounds/spin.ogg";
    private const string SfxCrate = "assets/sounds/crate.ogg";
    private const string SfxWumpa = "assets/sounds/wumpa.ogg";
    private const string SfxHurt = "assets/sounds/hurt.ogg";
    private const string SfxMask = "assets/sounds/mask.ogg";
    private const string SfxDie = "assets/sounds/die.ogg";
    private const string SfxCheckpoint = "assets/sounds/checkpoint.ogg";
    private const string SfxWin = "assets/sounds/win.ogg";
    private const string SfxLife = "assets/sounds/life.ogg";
    private const string SfxBounce = "assets/sounds/bounce.ogg";
    private const string SfxStomp = "assets/sounds/stomp.ogg";
    private const string SfxFuse = "assets/sounds/fuse.ogg";

    private static readonly Vector3 ChaseEuler = new(-0.473f, 3.144f, 0f);
    private static readonly Vector3 SideEuler = new(-0.65f, -1.571f, 0.4f);
    private static readonly Vector3 ChaseOffset = new(0f, 3.4f, -6.5f);
    private static readonly Vector3 SideOffset = new(-7.4f, 2.5f, 0.2f);
    private static readonly Vector4 LitTint = new(1f, 0.35f, 0.25f, 1f);
    private static readonly Vector4 HeartFull = new(0.92f, 0.18f, 0.22f, 1f);
    private static readonly Vector4 HeartEmpty = new(0.25f, 0.18f, 0.2f, 1f);

    private bool _announced;

    public int Priority => 115;

    public void OnInit()
    {
        Console.WriteLine("Crash Like — auto-run into the screen. A/D strafe, Space jumps, J spins, R restarts.");
    }

    public void OnShutdown() { }

    public void OnUpdate(TimeSpan deltaTime)
    {
        var game = FindGame();
        if (game == null)
            return;

        if (!TryFind("Player", out var player)
            || !player.TryGetComponent<TransformComponent>(out var playerTransform)
            || !player.TryGetComponent<PlayerMotorComponent>(out var motor))
            return;

        if (!_announced)
        {
            _announced = true;
            Console.WriteLine("The chase camera sits behind you. One stretch swings it to the side. Pits ignore the mask.");
        }

        var dt = MathF.Min((float)deltaTime.TotalSeconds, 0.05f);

        if (keyboard.WasKeyPressed(KeyCodes.R))
        {
            Reset(game, playerTransform, motor);
            SyncWorld(game, playerTransform, motor);
            return;
        }

        if (game.Phase != CrashLikeGameComponent.Playing)
        {
            motor.VelocityX = 0f;
            motor.VelocityY = 0f;
            motor.VelocityZ = 0f;
            StopEnemies();
            FollowCamera(game, playerTransform, dt);
            SyncWorld(game, playerTransform, motor);
            return;
        }

        if (game.SpinTimer > 0f)
            game.SpinTimer -= dt;
        if (game.InvulnTimer > 0f)
            game.InvulnTimer -= dt;

        var zone = FindZone(playerTransform.Translation.Z);
        game.CameraMode = zone is { Mode: CrashLikeGameComponent.Side }
            ? CrashLikeGameComponent.Side
            : CrashLikeGameComponent.Chase;

        var left = keyboard.IsKeyDown(KeyCodes.A) || keyboard.IsKeyDown(KeyCodes.Left);
        var right = keyboard.IsKeyDown(KeyCodes.D) || keyboard.IsKeyDown(KeyCodes.Right);
        var jumpPressed = keyboard.WasKeyPressed(KeyCodes.Space)
            || keyboard.WasKeyPressed(KeyCodes.W)
            || keyboard.WasKeyPressed(KeyCodes.Up);
        var jumpHeld = keyboard.IsKeyDown(KeyCodes.Space)
            || keyboard.IsKeyDown(KeyCodes.W)
            || keyboard.IsKeyDown(KeyCodes.Up);

        if ((keyboard.WasKeyPressed(KeyCodes.J)
                || keyboard.WasKeyPressed(KeyCodes.Z)
                || keyboard.WasKeyPressed(KeyCodes.LeftShift))
            && game.SpinTimer <= 0f)
        {
            game.SpinTimer = game.SpinTime;
            audio.PlayOneShot(SfxSpin);
        }

        var accel = (motor.Grounded ? game.GroundAccel : game.AirAccel) * dt;
        motor.VelocityX = CrashLikeRules.MoveTowards(
            motor.VelocityX, CrashLikeRules.Strafe(left, right, game.StrafeSpeed), accel);
        motor.VelocityZ = CrashLikeRules.MoveTowards(motor.VelocityZ, game.MoveSpeed, accel);
        motor.VelocityY -= game.Gravity * dt;

        if (CrashLikeRules.TryConsumeJump(game, jumpPressed, motor.Grounded, dt))
        {
            motor.VelocityY = game.JumpSpeed;
            motor.Grounded = false;
            audio.PlayOneShot(SfxJump);
        }
        else if (game.JumpHeld && !jumpHeld && motor.VelocityY > 0f)
        {
            motor.VelocityY *= game.JumpCut;
        }

        game.JumpHeld = jumpHeld;

        var fallingSpeed = motor.VelocityY;
        var half = new Vector3(motor.HalfWidth, motor.HalfHeight, motor.HalfDepth);
        var pos = playerTransform.Translation;
        pos = MoveAxis(pos, half, axis: 0, motor.VelocityX * dt, out var hitX);
        pos = MoveAxis(pos, half, axis: 2, motor.VelocityZ * dt, out var hitZ);
        pos = MoveAxis(pos, half, axis: 1, motor.VelocityY * dt, out var hitY);
        if (hitX == AxisHit.Blocked)
            motor.VelocityX = 0f;
        if (hitZ == AxisHit.Blocked)
            motor.VelocityZ = 0f;
        if (hitY == AxisHit.Landed)
        {
            motor.VelocityY = 0f;
            motor.Grounded = true;
        }
        else if (hitY == AxisHit.Ceiling)
        {
            motor.VelocityY = 0f;
            motor.Grounded = false;
        }
        else
        {
            motor.Grounded = false;
        }

        playerTransform.Translation = pos;

        var spinning = game.SpinTimer > 0f;
        TickFuses(game, playerTransform, dt);
        var vy = ResolveCrates(game, pos, half, motor.VelocityY, fallingSpeed, spinning);
        ResolveEnemies(game, playerTransform, pos, half, fallingSpeed, ref vy, spinning);
        motor.VelocityY = vy;
        ResolvePickups(game, pos);
        ResolveHazards(game, playerTransform, pos, half);
        ResolveCheckpoints(game, pos);

        if (game.Phase == CrashLikeGameComponent.Playing && playerTransform.Translation.Y < game.DeathY)
            ApplyDamage(game, playerTransform, motor, CrashLikeRules.Fall(game), pit: true);

        if (game.Phase == CrashLikeGameComponent.Playing)
            StepEnemies(dt);
        else
        {
            motor.VelocityX = 0f;
            motor.VelocityY = 0f;
            motor.VelocityZ = 0f;
            StopEnemies();
        }

        if (MathF.Abs(playerTransform.Translation.Z - game.StartZ) > 1.6f)
            game.HintVisible = false;

        FollowCamera(game, playerTransform, dt);
        SyncWorld(game, playerTransform, motor);
    }

    private enum AxisHit { None, Blocked, Landed, Ceiling }

    private Vector3 MoveAxis(Vector3 pos, Vector3 half, int axis, float delta, out AxisHit hit)
    {
        hit = AxisHit.None;
        if (delta == 0f)
            return pos;

        pos[axis] += delta;
        var corrected = false;
        var best = delta > 0f ? float.PositiveInfinity : float.NegativeInfinity;

        void Consider(Vector3 other, Vector3 oh)
        {
            if (!CrashLikeRules.Overlap3(pos, half, other, oh))
                return;

            var limit = delta > 0f
                ? other[axis] - oh[axis] - half[axis] - 0.001f
                : other[axis] + oh[axis] + half[axis] + 0.001f;
            if (delta > 0f && limit < best)
                best = limit;
            if (delta < 0f && limit > best)
                best = limit;
            corrected = true;
        }

        foreach (var (entity, solid) in context.View<SolidComponent>())
        {
            if (!Blocks(entity))
                continue;
            Consider(Position(entity), new Vector3(solid.HalfX, solid.HalfY, solid.HalfZ));
        }

        foreach (var (entity, enemy) in context.View<EnemyComponent>())
        {
            if (!enemy.Alive)
                continue;
            Consider(Position(entity), new Vector3(enemy.HalfWidth, enemy.HalfHeight, enemy.HalfDepth));
        }

        if (!corrected || float.IsInfinity(best))
            return pos;

        pos[axis] = best;
        hit = axis == 1
            ? delta < 0f ? AxisHit.Landed : AxisHit.Ceiling
            : AxisHit.Blocked;
        return pos;
    }

    private bool Blocks(Entity entity)
    {
        if (entity.TryGetComponent<CrateComponent>(out var crate) && crate.Broken)
            return false;
        return entity.HasComponent<SolidComponent>();
    }

    private void TickFuses(CrashLikeGameComponent game, TransformComponent playerTransform, float dt)
    {
        var exploded = false;
        foreach (var (entity, crate) in context.View<CrateComponent>())
        {
            if (!crate.Lit || crate.Broken || game.Phase != CrashLikeGameComponent.Playing)
                continue;

            crate.FuseTimer -= dt;
            if (crate.FuseTimer > 0f)
                continue;

            var origin = Position(entity);
            RetireCrate(entity, crate);
            exploded = true;
            audio.PlayOneShot(SfxCrate);
            var delta = playerTransform.Translation - new Vector3(origin.X, origin.Y, origin.Z);
            if (delta.LengthSquared() <= 1.7f * 1.7f)
                ApplyDamage(game, playerTransform, Motor(playerTransform), CrashLikeRules.TakeDamage(game), pit: false);
        }

        if (exploded)
            SettleCrates();
    }

    private float ResolveCrates(
        CrashLikeGameComponent game,
        Vector3 pos,
        Vector3 half,
        float vy,
        float fallingSpeed,
        bool spinning)
    {
        var settled = false;
        foreach (var (entity, crate) in context.View<CrateComponent>())
        {
            if (crate.Broken || game.Phase != CrashLikeGameComponent.Playing)
                continue;

            var cratePos = Position(entity);
            var crateHalf = new Vector3(crate.HalfWidth, crate.HalfHeight, crate.HalfDepth);
            if (!CrashLikeRules.Overlap3(pos, half, cratePos, crateHalf, spinning ? 0.22f : 0.04f))
                continue;

            var stomp = CrashLikeRules.IsStomp(pos.Y, half.Y, fallingSpeed, cratePos.Y, crate.HalfHeight)
                && MathF.Abs(pos.X - cratePos.X) <= half.X + crate.HalfWidth
                && MathF.Abs(pos.Z - cratePos.Z) <= half.Z + crate.HalfDepth;

            if (crate.CrateType == CrateComponent.Bounce)
            {
                if (fallingSpeed < -0.35f && stomp)
                {
                    vy = game.BounceSpeed;
                    audio.PlayOneShot(SfxBounce);
                }

                continue;
            }

            if (crate.CrateType == CrateComponent.Tnt)
            {
                if ((spinning || stomp) && !crate.Lit)
                {
                    crate.Lit = true;
                    crate.FuseTimer = 0.7f;
                    audio.PlayOneShot(SfxFuse);
                    if (stomp)
                        vy = MathF.Max(vy, game.StompBounce);
                }

                continue;
            }

            if (!spinning && !stomp)
                continue;

            BreakCrate(game, entity, crate);
            settled = true;
            if (stomp)
                vy = MathF.Max(vy, game.StompBounce);
        }

        if (settled)
            SettleCrates();
        return vy;
    }

    private void ResolveEnemies(
        CrashLikeGameComponent game,
        TransformComponent playerTransform,
        Vector3 pos,
        Vector3 half,
        float fallingSpeed,
        ref float vy,
        bool spinning)
    {
        foreach (var (entity, enemy) in context.View<EnemyComponent>())
        {
            if (!enemy.Alive || game.Phase != CrashLikeGameComponent.Playing)
                continue;

            var enemyPos = Position(entity);
            var enemyHalf = new Vector3(enemy.HalfWidth, enemy.HalfHeight, enemy.HalfDepth);
            if (!CrashLikeRules.Overlap3(pos, half, enemyPos, enemyHalf, spinning ? 0.2f : 0.02f))
                continue;

            var stomp = CrashLikeRules.IsStomp(pos.Y, half.Y, fallingSpeed, enemyPos.Y, enemy.HalfHeight);
            if (spinning || stomp)
            {
                KillEnemy(entity, enemy);
                audio.PlayOneShot(SfxStomp);
                if (stomp)
                    vy = MathF.Max(vy, game.StompBounce);
                continue;
            }

            ApplyDamage(game, playerTransform, Motor(playerTransform), CrashLikeRules.TakeDamage(game), pit: false);
        }
    }

    private void ResolvePickups(CrashLikeGameComponent game, Vector3 pos)
    {
        foreach (var (_, pickup) in context.View<CollectibleComponent>())
        {
            if (pickup.Taken || game.Phase != CrashLikeGameComponent.Playing)
                continue;

            var dx = pos.X - pickup.HomeX;
            var dy = pos.Y - pickup.HomeY;
            var dz = pos.Z - pickup.HomeZ;
            if (dx * dx + dy * dy + dz * dz > pickup.Radius * pickup.Radius)
                continue;

            pickup.Taken = true;
            switch (pickup.PickupType)
            {
                case CollectibleComponent.Wumpa:
                    var lives = CrashLikeRules.AddWumpa(game, pickup.Value);
                    audio.PlayOneShot(SfxWumpa);
                    if (lives > 0)
                        audio.PlayOneShot(SfxLife);
                    break;
                case CollectibleComponent.Life:
                    if (game.Lives < game.MaxLives)
                        game.Lives++;
                    audio.PlayOneShot(SfxLife);
                    break;
                case CollectibleComponent.Mask:
                    game.Masks = 1;
                    audio.PlayOneShot(SfxMask);
                    Console.WriteLine("Mask up. The next hit is absorbed.");
                    break;
                case CollectibleComponent.Goal:
                    game.Phase = CrashLikeGameComponent.Complete;
                    audio.PlayOneShot(SfxWin);
                    Console.WriteLine("Level clear. Press R to run it again.");
                    break;
            }
        }
    }

    private void ResolveHazards(CrashLikeGameComponent game, TransformComponent playerTransform, Vector3 pos, Vector3 half)
    {
        foreach (var (entity, hazard) in context.View<HazardComponent>())
        {
            if (game.Phase != CrashLikeGameComponent.Playing)
                return;

            var hazardPos = Position(entity);
            var hazardHalf = new Vector3(hazard.HalfWidth, hazard.HalfHeight, hazard.HalfDepth);
            if (!CrashLikeRules.Overlap3(pos, half, hazardPos, hazardHalf))
                continue;

            ApplyDamage(game, playerTransform, Motor(playerTransform), CrashLikeRules.TakeDamage(game), pit: false);
        }
    }

    private void ResolveCheckpoints(CrashLikeGameComponent game, Vector3 pos)
    {
        foreach (var (entity, checkpoint) in context.View<CheckpointComponent>())
        {
            if (checkpoint.Activated || game.Phase != CrashLikeGameComponent.Playing)
                continue;

            var checkpointPos = Position(entity);
            var checkpointHalf = new Vector3(checkpoint.HalfWidth, checkpoint.HalfHeight, checkpoint.HalfDepth);
            if (!CrashLikeRules.Overlap3(pos, new Vector3(0.2f, 0.2f, 0.2f), checkpointPos, checkpointHalf))
                continue;

            checkpoint.Activated = true;
            game.RespawnX = checkpoint.SpawnX;
            game.RespawnY = checkpoint.SpawnY;
            game.RespawnZ = checkpoint.SpawnZ;
            audio.PlayOneShot(SfxCheckpoint);
            Console.WriteLine($"Checkpoint {checkpoint.Id}.");
        }
    }

    private void ApplyDamage(
        CrashLikeGameComponent game,
        TransformComponent playerTransform,
        PlayerMotorComponent motor,
        DamageKind kind,
        bool pit)
    {
        switch (kind)
        {
            case DamageKind.Masked:
                audio.PlayOneShot(SfxMask);
                Console.WriteLine("Mask shattered.");
                break;
            case DamageKind.Hurt:
                audio.PlayOneShot(pit ? SfxDie : SfxHurt);
                Console.WriteLine($"Down. Lives: {game.Lives}");
                playerTransform.Translation = new Vector3(game.RespawnX, game.RespawnY, game.RespawnZ);
                motor.VelocityX = 0f;
                motor.VelocityY = 0f;
                motor.VelocityZ = 0f;
                game.SpinTimer = 0f;
                break;
            case DamageKind.Dead:
                audio.PlayOneShot(SfxDie);
                Console.WriteLine("Game over. Press R.");
                motor.VelocityX = 0f;
                motor.VelocityY = 0f;
                motor.VelocityZ = 0f;
                break;
        }
    }

    private PlayerMotorComponent Motor(TransformComponent playerTransform)
    {
        foreach (var (entity, motor) in context.View<PlayerMotorComponent>())
        {
            if (entity.TryGetComponent<TransformComponent>(out var transform) && ReferenceEquals(transform, playerTransform))
                return motor;
        }

        foreach (var (_, motor) in context.View<PlayerMotorComponent>())
            return motor;

        throw new InvalidOperationException("Player motor missing.");
    }

    private void BreakCrate(CrashLikeGameComponent game, Entity entity, CrateComponent crate)
    {
        if (crate.Broken)
            return;

        RetireCrate(entity, crate);
        if (crate.BonusLife && game.Lives < game.MaxLives)
        {
            game.Lives++;
            audio.PlayOneShot(SfxLife);
        }

        var lives = CrashLikeRules.AddWumpa(game, crate.LootWumpa);
        audio.PlayOneShot(SfxCrate);
        if (lives > 0)
            audio.PlayOneShot(SfxLife);
    }

    private static void RetireCrate(Entity entity, CrateComponent crate)
    {
        crate.Broken = true;
        crate.Lit = false;
        if (entity.TryGetComponent<TransformComponent>(out var transform))
            transform.Translation = new Vector3(crate.HomeX, -40f, crate.HomeZ);
    }

    private void SettleCrates()
    {
        for (var pass = 0; pass < 4; pass++)
        {
            var moved = false;
            foreach (var (entity, crate) in context.View<CrateComponent>())
            {
                if (crate.Broken || crate.CrateType == CrateComponent.Bounce)
                    continue;

                var pos = Position(entity);
                var floor = 0f;
                foreach (var (otherEntity, other) in context.View<CrateComponent>())
                {
                    if (other.Broken || otherEntity.Id == entity.Id)
                        continue;

                    var otherPos = Position(otherEntity);
                    if (MathF.Abs(otherPos.X - pos.X) > crate.HalfWidth + other.HalfWidth - 0.05f)
                        continue;
                    if (MathF.Abs(otherPos.Z - pos.Z) > crate.HalfDepth + other.HalfDepth - 0.05f)
                        continue;

                    var top = otherPos.Y + other.HalfHeight;
                    if (top <= pos.Y - crate.HalfHeight + 0.05f && top > floor)
                        floor = top;
                }

                var desired = floor + crate.HalfHeight;
                if (desired >= pos.Y - 0.02f)
                    continue;

                if (entity.TryGetComponent<TransformComponent>(out var transform))
                    transform.Translation = new Vector3(pos.X, desired, pos.Z);
                moved = true;
            }

            if (!moved)
                break;
        }
    }

    private static void KillEnemy(Entity entity, EnemyComponent enemy)
    {
        enemy.Alive = false;
        if (entity.TryGetComponent<TransformComponent>(out var transform))
            transform.Translation = new Vector3(enemy.HomeX, -40f, enemy.HomeZ);
    }

    private void StepEnemies(float dt)
    {
        if (!TryFind("Player", out var player) || !player.TryGetComponent<TransformComponent>(out var playerTransform))
            return;

        var playerPos = playerTransform.Translation;
        foreach (var (entity, enemy) in context.View<EnemyComponent>())
        {
            if (!enemy.Alive || !entity.TryGetComponent<TransformComponent>(out var transform))
                continue;

            var velocity = CrashLikeRules.StepEnemy(enemy, transform.Translation.X, playerPos.X, playerPos.Y);
            var x = transform.Translation.X + velocity.X * dt;
            transform.Translation = new Vector3(x, enemy.HomeY, enemy.HomeZ);
        }
    }

    private void StopEnemies()
    {
        foreach (var (entity, enemy) in context.View<EnemyComponent>())
        {
            if (!enemy.Alive && entity.TryGetComponent<TransformComponent>(out var transform))
                transform.Translation = new Vector3(enemy.HomeX, -40f, enemy.HomeZ);
        }
    }

    private void Reset(CrashLikeGameComponent game, TransformComponent playerTransform, PlayerMotorComponent motor)
    {
        if (game.StartingLives <= 0)
            game.StartingLives = 3;

        game.Phase = CrashLikeGameComponent.Playing;
        game.Lives = game.StartingLives;
        game.Wumpa = 0;
        game.Masks = 0;
        game.SpinTimer = 0f;
        game.InvulnTimer = 0f;
        game.CoyoteTimer = 0f;
        game.JumpBuffer = 0f;
        game.JumpHeld = false;
        game.CameraMode = CrashLikeGameComponent.Chase;
        game.RespawnX = game.StartX;
        game.RespawnY = game.StartY;
        game.RespawnZ = game.StartZ;
        game.HintVisible = true;
        motor.VelocityX = 0f;
        motor.VelocityY = 0f;
        motor.VelocityZ = 0f;
        motor.Grounded = false;
        playerTransform.Translation = new Vector3(game.StartX, game.StartY, game.StartZ);

        foreach (var (entity, crate) in context.View<CrateComponent>())
        {
            crate.Broken = false;
            crate.Lit = false;
            crate.FuseTimer = 0f;
            if (entity.TryGetComponent<TransformComponent>(out var transform))
                transform.Translation = new Vector3(crate.HomeX, crate.HomeY, crate.HomeZ);
        }

        foreach (var (entity, enemy) in context.View<EnemyComponent>())
        {
            enemy.Alive = true;
            enemy.Direction = 1;
            if (entity.TryGetComponent<TransformComponent>(out var transform))
                transform.Translation = new Vector3(enemy.HomeX, enemy.HomeY, enemy.HomeZ);
        }

        foreach (var (entity, pickup) in context.View<CollectibleComponent>())
        {
            pickup.Taken = false;
            if (entity.TryGetComponent<TransformComponent>(out var transform))
                transform.Translation = new Vector3(pickup.HomeX, pickup.HomeY, pickup.HomeZ);
        }

        foreach (var (_, checkpoint) in context.View<CheckpointComponent>())
            checkpoint.Activated = false;

        Console.WriteLine("Level reset.");
    }

    private CameraZoneComponent? FindZone(float z)
    {
        CameraZoneComponent? found = null;
        foreach (var (_, zone) in context.View<CameraZoneComponent>())
        {
            if (z >= zone.MinZ && z <= zone.MaxZ)
                found = zone;
        }

        return found;
    }

    private void FollowCamera(CrashLikeGameComponent game, TransformComponent playerTransform, float dt)
    {
        if (!TryFind("Camera", out var camera)
            || !camera.TryGetComponent<TransformComponent>(out var cameraTransform)
            || !camera.TryGetComponent<CameraComponent>(out var cameraComponent))
            return;

        cameraComponent.CameraViewTransform = null;
        var side = game.CameraMode == CrashLikeGameComponent.Side;
        var offset = side ? SideOffset : ChaseOffset;
        var euler = side ? SideEuler : ChaseEuler;
        var desired = playerTransform.Translation + offset;
        var current = cameraTransform.Translation;
        var t = 1f - MathF.Exp(-game.CameraSharpness * dt);
        cameraTransform.Translation = current + (desired - current) * t;
        cameraTransform.Rotation = euler;
    }

    private void SyncWorld(CrashLikeGameComponent game, TransformComponent playerTransform, PlayerMotorComponent motor)
    {
        var spinYaw = game.SpinTimer > 0f ? game.SpinTimer * 28f : 0f;
        var cos = MathF.Cos(spinYaw);
        var sin = MathF.Sin(spinYaw);
        foreach (var (entity, rider) in context.View<RiderComponent>())
        {
            if (!entity.TryGetComponent<TransformComponent>(out var transform))
                continue;

            var x = rider.LocalX * cos + rider.LocalZ * sin;
            var z = -rider.LocalX * sin + rider.LocalZ * cos;
            transform.Translation = playerTransform.Translation + new Vector3(x, rider.LocalY, z);
            transform.Rotation = new Vector3(motor.Grounded ? 0f : -0.35f, spinYaw, 0f);
        }

        if (TryFind("Player", out var player) && player.TryGetComponent<TransformComponent>(out var body))
            body.Rotation = new Vector3(motor.Grounded ? 0f : -0.35f, spinYaw, 0f);

        foreach (var (entity, crate) in context.View<CrateComponent>())
        {
            if (!entity.TryGetComponent<ModelRendererComponent>(out var model))
                continue;

            if (crate.Broken)
            {
                if (entity.TryGetComponent<TransformComponent>(out var transform))
                    transform.Translation = new Vector3(crate.HomeX, -40f, crate.HomeZ);
                continue;
            }

            if (crate.Lit && ((int)(crate.FuseTimer * 16f) & 1) == 0)
                model.Color = LitTint;
        }

        foreach (var (entity, enemy) in context.View<EnemyComponent>())
        {
            if (!enemy.Alive && entity.TryGetComponent<TransformComponent>(out var transform))
                transform.Translation = new Vector3(enemy.HomeX, -40f, enemy.HomeZ);
        }

        foreach (var (entity, checkpoint) in context.View<CheckpointComponent>())
        {
            if (!entity.TryGetComponent<ModelRendererComponent>(out var flag))
                continue;
            flag.Color = checkpoint.Activated
                ? new Vector4(0.95f, 0.82f, 0.2f, 1f)
                : new Vector4(0.2f, 0.72f, 0.38f, 1f);
        }

        foreach (var (entity, pickup) in context.View<CollectibleComponent>())
        {
            if (!entity.TryGetComponent<TransformComponent>(out var transform))
                continue;

            transform.Translation = pickup.Taken
                ? new Vector3(pickup.HomeX, -40f, pickup.HomeZ)
                : new Vector3(pickup.HomeX, pickup.HomeY, pickup.HomeZ);
        }

        SyncHud(game);
    }

    private void SyncHud(CrashLikeGameComponent game)
    {
        if (!TryFind("Camera", out var camera) || !camera.TryGetComponent<TransformComponent>(out var cameraTransform))
            return;

        var euler = cameraTransform.Rotation;
        var rotation = MathHelpers.MatrixFromQuaternion(MathHelpers.QuaternionFromEuler(euler));
        var forward = Vector3.Normalize(Vector3.TransformNormal(-Vector3.UnitZ, rotation));
        var right = Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitX, rotation));
        var up = Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitY, rotation));
        var origin = cameraTransform.Translation + forward * 1.85f;

        for (var i = 0; i < 5; i++)
        {
            if (!TryFind($"Heart_{i}", out var heart) || !heart.TryGetComponent<TransformComponent>(out var transform))
                continue;

            transform.Translation = origin + right * (-1.15f + i * 0.16f) + up * 0.62f;
            transform.Rotation = euler;
            if (heart.TryGetComponent<ModelRendererComponent>(out var model))
                model.Color = i < game.Lives ? HeartFull : HeartEmpty;
        }

        PlaceHud("WumpaIcon", origin + right * -0.15f + up * 0.62f, euler, true);
        var tens = System.Math.Abs(game.Wumpa) / 10;
        var ones = System.Math.Abs(game.Wumpa) % 10;
        foreach (var (entity, digit) in context.View<HudDigitComponent>())
        {
            if (!entity.TryGetComponent<TransformComponent>(out var transform))
                continue;

            transform.Translation = origin + right * (0.08f + digit.Place * 0.16f) + up * 0.62f;
            transform.Rotation = euler;
            if (entity.TryGetComponent<ModelRendererComponent>(out var model))
            {
                var value = digit.Place == 0 ? tens : ones;
                model.TexturePath = $"textures/hud/{value % 10}.png";
                model.Color = Vector4.One;
            }
        }

        PlaceHud("MaskIcon", origin + right * 0.55f + up * 0.62f, euler, game.Masks > 0);
        PlaceHud("Hint", origin + up * 0.15f, euler, game.HintVisible && game.Phase == CrashLikeGameComponent.Playing);
        PlaceHud("BannerGameOver", origin, euler, game.Phase == CrashLikeGameComponent.GameOver);
        PlaceHud("BannerClear", origin, euler, game.Phase == CrashLikeGameComponent.Complete);
    }

    private void PlaceHud(string name, Vector3 position, Vector3 euler, bool visible)
    {
        if (!TryFind(name, out var entity) || !entity.TryGetComponent<TransformComponent>(out var transform))
            return;

        transform.Translation = visible ? position : new Vector3(0f, -50f, 0f);
        transform.Rotation = euler;
    }

    private CrashLikeGameComponent? FindGame()
    {
        foreach (var (_, game) in context.View<CrashLikeGameComponent>())
            return game;
        return null;
    }

    private bool TryFind(string name, out Entity entity)
    {
        foreach (var candidate in context.Entities)
        {
            if (candidate.Name != name)
                continue;

            entity = candidate;
            return true;
        }

        entity = null!;
        return false;
    }

    private static Vector3 Position(Entity entity)
    {
        if (!entity.TryGetComponent<TransformComponent>(out var transform))
            return Vector3.Zero;
        return transform.Translation;
    }
}
