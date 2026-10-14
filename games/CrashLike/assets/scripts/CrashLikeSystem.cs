using System.Numerics;
using Audio;
using ECS;
using ECS.Systems;
using Input;
using SceneComponents;
using SceneComponents.Camera;
using SceneComponents.Physics;
using SceneComponents.Rendering;
using Scripting;

namespace CrashLike.assets.scripts;

/// <summary>
/// Side-view platformer homage: run, jump, spin, crates, mask, and an auto-run corridor.
/// A/D or arrows move, Space/W/Up jump, J/Z/Left Shift spin, R restarts.
/// </summary>
[Register(typeof(IGameSystem))]
public class CrashLikeSystem(
    Context context,
    IKeyboardInput keyboard,
    IPhysicsQueries physics,
    IAudio audio) : IGameSystem
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

    private static readonly Vector4 Hidden = new(1f, 1f, 1f, 0f);
    private static readonly Vector4 HeartEmpty = new(0.35f, 0.35f, 0.42f, 0.45f);
    private static readonly Vector4 SpinTint = new(1f, 0.72f, 0.28f, 1f);
    private static readonly Vector4 LitTint = new(1f, 0.35f, 0.25f, 1f);

    private bool _announced;

    public int Priority => 115;

    public void OnInit()
    {
        Console.WriteLine("Crash Like — A/D or arrows move, Space jumps, J spins, R restarts.");
    }

    public void OnShutdown() { }

    public void OnUpdate(TimeSpan deltaTime)
    {
        var game = FindGame();
        if (game == null)
            return;

        if (!TryFind("Player", out var player)
            || !player.TryGetComponent<TransformComponent>(out var playerTransform)
            || !player.TryGetComponent<RigidBody2DComponent>(out var body)
            || !player.TryGetComponent<PlayerMotorComponent>(out var motor))
            return;

        if (!_announced)
        {
            _announced = true;
            Console.WriteLine("Break crates with a jump or a spin. Pits ignore the mask. The marked stretch auto-runs.");
        }

        var dt = MathF.Min((float)deltaTime.TotalSeconds, 0.05f);

        if (keyboard.WasKeyPressed(KeyCodes.R))
        {
            Reset(game, playerTransform, body, motor);
            SyncWorld(game, player, motor, grounded: true);
            return;
        }

        if (game.TeleportPending)
        {
            body.BodyType = RigidBodyType.Dynamic;
            body.Velocity = Vector2.Zero;
            game.TeleportPending = false;
            SyncWorld(game, player, motor, grounded: true);
            return;
        }

        if (game.Phase != CrashLikeGameComponent.Playing)
        {
            Freeze(body);
            StopEnemies();
            SyncWorld(game, player, motor, grounded: false);
            return;
        }

        if (game.SpinTimer > 0f)
            game.SpinTimer -= dt;
        if (game.InvulnTimer > 0f)
            game.InvulnTimer -= dt;

        var px = playerTransform.Translation.X;
        var py = playerTransform.Translation.Y;
        var zone = FindZone(px);
        var depth = zone is { Mode: CrashLikeGameComponent.Depth };
        game.CameraMode = depth ? CrashLikeGameComponent.Depth : CrashLikeGameComponent.Side;

        var grounded = IsGrounded(player, playerTransform, motor);
        var jumpPressed = keyboard.WasKeyPressed(KeyCodes.Space)
            || keyboard.WasKeyPressed(KeyCodes.W)
            || keyboard.WasKeyPressed(KeyCodes.Up);
        var jumpHeld = keyboard.IsKeyDown(KeyCodes.Space)
            || keyboard.IsKeyDown(KeyCodes.W)
            || keyboard.IsKeyDown(KeyCodes.Up);
        var left = keyboard.IsKeyDown(KeyCodes.A) || keyboard.IsKeyDown(KeyCodes.Left);
        var right = keyboard.IsKeyDown(KeyCodes.D) || keyboard.IsKeyDown(KeyCodes.Right);

        if ((keyboard.WasKeyPressed(KeyCodes.J)
                || keyboard.WasKeyPressed(KeyCodes.Z)
                || keyboard.WasKeyPressed(KeyCodes.LeftShift))
            && game.SpinTimer <= 0f)
        {
            game.SpinTimer = game.SpinTime;
            audio.PlayOneShot(SfxSpin);
        }

        var vx = body.Velocity.X;
        var vy = body.Velocity.Y;
        var fallingSpeed = vy;
        vx = CrashLikeRules.MoveTowards(
            vx,
            CrashLikeRules.TargetSpeed(depth, left, right, game.MoveSpeed),
            (grounded ? game.GroundAccel : game.AirAccel) * dt);

        if (depth)
            motor.Facing = 1;
        else if (vx > 0.15f)
            motor.Facing = 1;
        else if (vx < -0.15f)
            motor.Facing = -1;

        if (CrashLikeRules.TryConsumeJump(game, jumpPressed, grounded, dt))
        {
            vy = game.JumpSpeed;
            audio.PlayOneShot(SfxJump);
        }
        else if (game.JumpHeld && !jumpHeld && vy > 0f)
        {
            vy *= game.JumpCut;
        }

        game.JumpHeld = jumpHeld;
        var spinning = game.SpinTimer > 0f;

        TickFuses(game, playerTransform, body, dt);
        vy = ResolveCrates(game, px, py, motor, vy, fallingSpeed, spinning);
        ResolveEnemies(game, playerTransform, body, motor, px, py, fallingSpeed, ref vy, spinning);
        ResolvePickups(game, px, py);
        ResolveHazards(game, playerTransform, body, motor, px, py);
        ResolveCheckpoints(game, px, py);

        if (StillPlaying(game) && playerTransform.Translation.Y < game.DeathY)
            ApplyDamage(game, playerTransform, body, CrashLikeRules.Fall(game), pit: true);

        if (game.TeleportPending)
            body.Velocity = Vector2.Zero;
        else if (game.Phase != CrashLikeGameComponent.Playing)
            Freeze(body);
        else
            body.Velocity = new Vector2(vx, vy);

        if (StillPlaying(game))
            StepEnemies(px, py);
        else
            StopEnemies();

        if (MathF.Abs(px - game.StartX) > 1.4f)
            game.HintVisible = false;

        FollowCamera(game, playerTransform, motor, dt);
        SyncWorld(game, player, motor, grounded);
    }

    private void TickFuses(
        CrashLikeGameComponent game,
        TransformComponent playerTransform,
        RigidBody2DComponent body,
        float dt)
    {
        var exploded = false;
        foreach (var (entity, crate) in context.View<CrateComponent>())
        {
            if (!crate.Lit || crate.Broken || !StillPlaying(game))
                continue;

            crate.FuseTimer -= dt;
            if (crate.FuseTimer > 0f)
                continue;

            var origin = Position(entity);
            RetireCrate(entity, crate);
            exploded = true;
            audio.PlayOneShot(SfxCrate);

            var dx = playerTransform.Translation.X - origin.X;
            var dy = playerTransform.Translation.Y - origin.Y;
            if (dx * dx + dy * dy <= 1.7f * 1.7f)
                ApplyDamage(game, playerTransform, body, CrashLikeRules.TakeDamage(game), pit: false);
        }

        if (exploded)
            SettleCrates();
    }

    private float ResolveCrates(
        CrashLikeGameComponent game,
        float px,
        float py,
        PlayerMotorComponent motor,
        float vy,
        float fallingSpeed,
        bool spinning)
    {
        var settled = false;
        foreach (var (entity, crate) in context.View<CrateComponent>())
        {
            if (crate.Broken || !StillPlaying(game))
                continue;

            var pos = Position(entity);
            if (!CrashLikeRules.Aabb(
                    px, py, motor.HalfWidth, motor.HalfHeight,
                    pos.X, pos.Y, crate.HalfWidth, crate.HalfHeight,
                    spinning ? 0.22f : 0.04f))
                continue;

            var stomp = CrashLikeRules.IsStomp(py, motor.HalfHeight, fallingSpeed, pos.Y, crate.HalfHeight);
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
            if (stomp || py - motor.HalfHeight >= pos.Y)
                vy = MathF.Max(vy, game.StompBounce);
        }

        if (settled)
            SettleCrates();

        return vy;
    }

    private void ResolveEnemies(
        CrashLikeGameComponent game,
        TransformComponent playerTransform,
        RigidBody2DComponent body,
        PlayerMotorComponent motor,
        float px,
        float py,
        float fallingSpeed,
        ref float vy,
        bool spinning)
    {
        foreach (var (entity, enemy) in context.View<EnemyComponent>())
        {
            if (!enemy.Alive || !StillPlaying(game))
                continue;

            var pos = Position(entity);
            if (!CrashLikeRules.Aabb(
                    px, py, motor.HalfWidth, motor.HalfHeight,
                    pos.X, pos.Y, enemy.HalfWidth, enemy.HalfHeight,
                    spinning ? 0.26f : 0.06f))
                continue;

            var stomp = CrashLikeRules.IsStomp(py, motor.HalfHeight, fallingSpeed, pos.Y, enemy.HalfHeight);
            if (spinning || stomp)
            {
                KillEnemy(entity, enemy);
                audio.PlayOneShot(SfxStomp);
                if (stomp)
                    vy = MathF.Max(vy, game.StompBounce);
                continue;
            }

            ApplyDamage(game, playerTransform, body, CrashLikeRules.TakeDamage(game), pit: false);
        }
    }

    private void ResolvePickups(CrashLikeGameComponent game, float px, float py)
    {
        foreach (var (entity, pickup) in context.View<CollectibleComponent>())
        {
            if (pickup.Taken || !StillPlaying(game))
                continue;

            var pos = Position(entity);
            var dx = px - pos.X;
            var dy = py - pos.Y;
            if (dx * dx + dy * dy > pickup.Radius * pickup.Radius)
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

    private void ResolveHazards(
        CrashLikeGameComponent game,
        TransformComponent playerTransform,
        RigidBody2DComponent body,
        PlayerMotorComponent motor,
        float px,
        float py)
    {
        foreach (var (entity, hazard) in context.View<HazardComponent>())
        {
            if (!StillPlaying(game))
                return;

            var pos = Position(entity);
            if (!CrashLikeRules.Aabb(
                    px, py, motor.HalfWidth, motor.HalfHeight,
                    pos.X, pos.Y, hazard.HalfWidth, hazard.HalfHeight))
                continue;

            ApplyDamage(game, playerTransform, body, CrashLikeRules.TakeDamage(game), pit: false);
        }
    }

    private void ResolveCheckpoints(CrashLikeGameComponent game, float px, float py)
    {
        foreach (var (entity, checkpoint) in context.View<CheckpointComponent>())
        {
            if (checkpoint.Activated || !StillPlaying(game))
                continue;

            var pos = Position(entity);
            if (!CrashLikeRules.Aabb(px, py, 0.2f, 0.2f, pos.X, pos.Y, checkpoint.HalfWidth, checkpoint.HalfHeight))
                continue;

            checkpoint.Activated = true;
            game.RespawnX = checkpoint.SpawnX;
            game.RespawnY = checkpoint.SpawnY;
            audio.PlayOneShot(SfxCheckpoint);
            Console.WriteLine($"Checkpoint {checkpoint.Id}.");
        }
    }

    private void ApplyDamage(
        CrashLikeGameComponent game,
        TransformComponent playerTransform,
        RigidBody2DComponent body,
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
                ArmTeleport(game, playerTransform, body);
                break;
            case DamageKind.Dead:
                audio.PlayOneShot(SfxDie);
                Console.WriteLine("Game over. Press R.");
                break;
        }
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
            transform.Translation = new Vector3(crate.HomeX, -40f, 0f);
        BumpStatic(entity);
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

                    var top = otherPos.Y + other.HalfHeight;
                    if (top <= pos.Y - crate.HalfHeight + 0.05f && top > floor)
                        floor = top;
                }

                var desired = floor + crate.HalfHeight;
                if (desired >= pos.Y - 0.02f)
                    continue;

                if (entity.TryGetComponent<TransformComponent>(out var transform))
                    transform.Translation = new Vector3(pos.X, desired, transform.Translation.Z);
                BumpStatic(entity);
                moved = true;
            }

            if (!moved)
                break;
        }
    }

    private static void BumpStatic(Entity entity)
    {
        if (!entity.TryGetComponent<BoxCollider2DComponent>(out var box))
            return;

        box.Offset = new Vector2(box.Offset.X + 0.02f, box.Offset.Y);
    }

    private static void KillEnemy(Entity entity, EnemyComponent enemy)
    {
        enemy.Alive = false;
        if (entity.TryGetComponent<TransformComponent>(out var transform))
            transform.Translation = new Vector3(enemy.HomeX, -40f, 0f);
        if (entity.TryGetComponent<RigidBody2DComponent>(out var body))
            body.Velocity = Vector2.Zero;
    }

    private void StepEnemies(float playerX, float playerY)
    {
        foreach (var (entity, enemy) in context.View<EnemyComponent>())
        {
            if (!enemy.Alive || !entity.TryGetComponent<RigidBody2DComponent>(out var body))
                continue;

            var x = entity.TryGetComponent<TransformComponent>(out var transform)
                ? transform.Translation.X
                : enemy.HomeX;
            body.Velocity = CrashLikeRules.StepEnemy(enemy, x, playerX, playerY);
        }
    }

    private void StopEnemies()
    {
        foreach (var (entity, _) in context.View<EnemyComponent>())
        {
            if (entity.TryGetComponent<RigidBody2DComponent>(out var body))
                body.Velocity = Vector2.Zero;
        }
    }

    private static void ArmTeleport(
        CrashLikeGameComponent game,
        TransformComponent playerTransform,
        RigidBody2DComponent body)
    {
        playerTransform.Translation = new Vector3(game.RespawnX, game.RespawnY, playerTransform.Translation.Z);
        body.Velocity = Vector2.Zero;
        body.BodyType = RigidBodyType.Kinematic;
        game.SpinTimer = 0f;
        game.TeleportPending = true;
    }

    private void Reset(
        CrashLikeGameComponent game,
        TransformComponent playerTransform,
        RigidBody2DComponent body,
        PlayerMotorComponent motor)
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
        game.CameraMode = CrashLikeGameComponent.Side;
        game.RespawnX = game.StartX;
        game.RespawnY = game.StartY;
        game.HintVisible = true;
        game.TeleportPending = true;
        motor.Facing = 1;

        playerTransform.Translation = new Vector3(game.StartX, game.StartY, playerTransform.Translation.Z);
        body.Velocity = Vector2.Zero;
        body.BodyType = RigidBodyType.Kinematic;

        foreach (var (entity, crate) in context.View<CrateComponent>())
        {
            crate.Broken = false;
            crate.Lit = false;
            crate.FuseTimer = 0f;
            if (entity.TryGetComponent<TransformComponent>(out var transform))
                transform.Translation = new Vector3(crate.HomeX, crate.HomeY, 0f);
            if (entity.TryGetComponent<BoxCollider2DComponent>(out var box))
                box.Offset = Vector2.Zero;
            if (entity.TryGetComponent<SpriteRendererComponent>(out var sprite))
                sprite.Color = Vector4.One;
        }

        foreach (var (entity, enemy) in context.View<EnemyComponent>())
        {
            enemy.Alive = true;
            enemy.Direction = 1;
            if (entity.TryGetComponent<TransformComponent>(out var transform))
                transform.Translation = new Vector3(enemy.HomeX, enemy.HomeY, 0f);
            if (entity.TryGetComponent<RigidBody2DComponent>(out var enemyBody))
                enemyBody.Velocity = Vector2.Zero;
        }

        foreach (var (_, pickup) in context.View<CollectibleComponent>())
            pickup.Taken = false;

        foreach (var (_, checkpoint) in context.View<CheckpointComponent>())
            checkpoint.Activated = false;

        if (TryFind("Camera", out var camera)
            && camera.TryGetComponent<CameraComponent>(out var cameraComponent)
            && camera.TryGetComponent<TransformComponent>(out var cameraTransform))
        {
            cameraComponent.OrthographicSize = game.SideOrtho;
            var half = game.SideOrtho * cameraComponent.AspectRatio;
            var x = CrashLikeRules.ClampCamera(
                game.StartX + game.SideLookAhead, half, game.LevelMinX, game.LevelMaxX);
            cameraTransform.Translation = new Vector3(x, game.CameraY, cameraTransform.Translation.Z);
        }

        Console.WriteLine("Level reset.");
    }

    private bool IsGrounded(Entity player, TransformComponent transform, PlayerMotorComponent motor)
    {
        var origin = new Vector2(transform.Translation.X, transform.Translation.Y);
        return physics.Raycast(origin, new Vector2(0f, -1f), motor.HalfHeight + 0.12f, player) != null;
    }

    private CameraZoneComponent? FindZone(float x)
    {
        CameraZoneComponent? found = null;
        foreach (var (_, zone) in context.View<CameraZoneComponent>())
        {
            if (x >= zone.MinX && x <= zone.MaxX)
                found = zone;
        }

        return found;
    }

    private void FollowCamera(
        CrashLikeGameComponent game,
        TransformComponent playerTransform,
        PlayerMotorComponent motor,
        float dt)
    {
        if (!TryFind("Camera", out var camera)
            || !camera.TryGetComponent<TransformComponent>(out var cameraTransform)
            || !camera.TryGetComponent<CameraComponent>(out var cameraComponent))
            return;

        var zone = FindZone(playerTransform.Translation.X);
        var depth = zone is { Mode: CrashLikeGameComponent.Depth };
        var look = depth ? zone!.LookAhead : game.SideLookAhead * motor.Facing;
        var ortho = depth ? zone!.OrthoSize : game.SideOrtho;
        cameraComponent.OrthographicSize = CrashLikeRules.MoveTowards(
            cameraComponent.OrthographicSize, ortho, 2.5f * dt);

        var half = cameraComponent.OrthographicSize * MathF.Max(0.1f, cameraComponent.AspectRatio);
        var desired = CrashLikeRules.ClampCamera(
            playerTransform.Translation.X + look, half, game.LevelMinX, game.LevelMaxX);
        var x = CrashLikeRules.Follow(cameraTransform.Translation.X, desired, game.CameraSharpness, dt);
        cameraTransform.Translation = new Vector3(x, game.CameraY, cameraTransform.Translation.Z);
    }

    private void SyncWorld(CrashLikeGameComponent game, Entity player, PlayerMotorComponent motor, bool grounded)
    {
        if (player.TryGetComponent<SpriteRendererComponent>(out var playerSprite))
        {
            var pose = game.SpinTimer > 0f ? "spin" : grounded ? "stand" : "jump";
            var side = motor.Facing < 0 ? "_left" : "";
            playerSprite.TexturePath = $"textures/player/{pose}{side}.png";
            var color = game.SpinTimer > 0f ? SpinTint : Vector4.One;
            if (game.InvulnTimer > 0f && ((int)(game.InvulnTimer * 14f) & 1) == 0)
                color.W = 0.28f;
            playerSprite.Color = color;
        }

        foreach (var (entity, crate) in context.View<CrateComponent>())
        {
            if (!entity.TryGetComponent<SpriteRendererComponent>(out var sprite))
                continue;

            if (crate.Broken)
            {
                sprite.Color = Hidden;
                continue;
            }

            sprite.Color = crate.Lit && ((int)(crate.FuseTimer * 16f) & 1) == 0 ? LitTint : Vector4.One;
        }

        foreach (var (entity, enemy) in context.View<EnemyComponent>())
        {
            if (!entity.TryGetComponent<SpriteRendererComponent>(out var sprite))
                continue;

            sprite.TexturePath = CrashLikeRules.FacingTexture(enemy.TexturePath, enemy.Direction);
            sprite.Color = enemy.Alive ? Vector4.One : Hidden;
        }

        foreach (var (entity, pickup) in context.View<CollectibleComponent>())
        {
            if (!entity.TryGetComponent<SpriteRendererComponent>(out var sprite))
                continue;

            sprite.Color = pickup.Taken ? Hidden : Vector4.One;
            if (!pickup.Taken && entity.TryGetComponent<TransformComponent>(out var transform))
                transform.Translation = new Vector3(pickup.HomeX, pickup.HomeY, transform.Translation.Z);
        }

        foreach (var (entity, checkpoint) in context.View<CheckpointComponent>())
        {
            if (!entity.TryGetComponent<SpriteRendererComponent>(out var sprite))
                continue;

            sprite.TexturePath = checkpoint.Activated ? checkpoint.ActiveTexture : checkpoint.IdleTexture;
            sprite.Color = Vector4.One;
        }

        SyncHud(game);
    }

    private void SyncHud(CrashLikeGameComponent game)
    {
        if (!TryFind("Camera", out var camera) || !camera.TryGetComponent<TransformComponent>(out var cameraTransform))
            return;

        var cam = cameraTransform.Translation;
        var halfH = 4.6f;
        var halfW = halfH * (16f / 9f);
        if (camera.TryGetComponent<CameraComponent>(out var cameraComponent))
        {
            halfH = cameraComponent.OrthographicSize;
            halfW = halfH * MathF.Max(0.1f, cameraComponent.AspectRatio);
        }

        var top = cam.Y + halfH - 0.48f;
        var left = cam.X - halfW + 0.48f;

        for (var i = 0; i < 5; i++)
        {
            if (!TryFind($"Heart_{i}", out var heart))
                continue;

            if (heart.TryGetComponent<TransformComponent>(out var transform))
                transform.Translation = new Vector3(left + i * 0.5f, top, 0.2f);
            if (heart.TryGetComponent<SpriteRendererComponent>(out var sprite))
                sprite.Color = i < game.Lives ? Vector4.One : HeartEmpty;
        }

        Place("WumpaIcon", left + 2.85f, top, 0.2f, visible: true);
        var tens = System.Math.Abs(game.Wumpa) / 10;
        var ones = System.Math.Abs(game.Wumpa) % 10;
        foreach (var (entity, digit) in context.View<HudDigitComponent>())
        {
            if (entity.TryGetComponent<TransformComponent>(out var transform))
                transform.Translation = new Vector3(left + 3.25f + digit.Place * 0.34f, top, 0.2f);
            if (entity.TryGetComponent<SpriteRendererComponent>(out var sprite))
            {
                var value = digit.Place == 0 ? tens : ones;
                sprite.TexturePath = $"textures/hud/{value % 10}.png";
                sprite.Color = Vector4.One;
            }
        }

        Place("MaskIcon", left + 4.15f, top, 0.2f, game.Masks > 0);
        Place("Hint", cam.X, cam.Y + halfH * 0.42f, 0.2f, game.HintVisible && game.Phase == CrashLikeGameComponent.Playing);
        Place("BannerGameOver", cam.X, cam.Y + 0.3f, 0.3f, game.Phase == CrashLikeGameComponent.GameOver);
        Place("BannerClear", cam.X, cam.Y + 0.3f, 0.3f, game.Phase == CrashLikeGameComponent.Complete);
    }

    private void Place(string name, float x, float y, float z, bool visible)
    {
        if (!TryFind(name, out var entity))
            return;

        if (entity.TryGetComponent<TransformComponent>(out var transform))
            transform.Translation = new Vector3(x, y, z);
        if (entity.TryGetComponent<SpriteRendererComponent>(out var sprite))
            sprite.Color = visible ? Vector4.One : Hidden;
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

    private static bool StillPlaying(CrashLikeGameComponent game) =>
        game.Phase == CrashLikeGameComponent.Playing && !game.TeleportPending;

    private static Vector2 Position(Entity entity)
    {
        if (!entity.TryGetComponent<TransformComponent>(out var transform))
            return Vector2.Zero;
        return new Vector2(transform.Translation.X, transform.Translation.Y);
    }

    private static void Freeze(RigidBody2DComponent body)
    {
        body.Velocity = Vector2.Zero;
        if (body.BodyType != RigidBodyType.Kinematic)
            body.BodyType = RigidBodyType.Kinematic;
    }
}
