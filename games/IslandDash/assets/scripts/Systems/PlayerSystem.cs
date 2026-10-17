using Audio;
using ECS;
using ECS.Systems;
using Input;
using SceneComponents;
using Scripting;

namespace IslandDash;

/// <summary>
/// Third-person platformer controller: camera-relative running, jump with coyote time + buffering,
/// variable height and double jump, spin attack, gravity and swept AABB collision (Y, then X, then Z).
/// </summary>
[Register(typeof(IGameSystem))]
public class PlayerSystem(Context context, IKeyboardInput keyboard, IMouseInput mouse, IAudio audio) : IGameSystem
{
    private const float FacingMinSpeed = 0.4f;
    private const float LandSoundImpact = 0.35f;
    private const float BlockedVelocityEpsilon = 1e-5f;

    public int Priority => SystemOrder.Player;

    public void OnInit() { }

    public void OnShutdown() { }

    public void OnUpdate(TimeSpan deltaTime)
    {
        if (World.State(context) is not { } state || World.Player(context) is not { } player)
            return;

        var dt = SystemOrder.ClampDelta(deltaTime);
        var pc = player.GetComponent<PlayerComponent>();
        var animator = player.GetComponent<ProceduralAnimatorComponent>();
        var tuning = World.Get<PlayerTuningComponent>(context);
        var settings = World.Get<GameSettingsComponent>(context);

        if (state.Phase != GamePhase.Playing)
        {
            animator.Spinning = false;
            animator.Speed01 = 0f;
            return;
        }

        pc.InvulnerableTime = MathF.Max(0f, pc.InvulnerableTime - dt);
        animator.Blink = pc.InvulnerableTime > 0f;

        var input = GameInput.Read(keyboard, mouse);
        var transform = player.GetComponent<TransformComponent>();

        RideGround(player, pc, transform);
        UpdateTimers(pc, input, tuning, dt);
        var wish = WishDirection(input.Move, state.CameraYaw);
        RunVelocity(pc, wish, tuning, dt);
        Jump(pc, input, tuning, animator);
        Spin(pc, input, tuning, animator);
        Gravity(pc, tuning, dt);
        Move(player, pc, transform, tuning, state, settings, animator, input, dt);

        var horizontal = new Vector2(pc.Velocity.X, pc.Velocity.Z);
        animator.Speed01 = System.Math.Clamp(horizontal.Length() / tuning.MaxRunSpeed, 0f, 1f);
        animator.Grounded = pc.Grounded;
        animator.VerticalSpeed = pc.Velocity.Y;
        animator.Spinning = pc.IsSpinning;
        if (wish.LengthSquared() > 0f)
            animator.FacingYaw = GameMath.YawFromDirection(wish);
        else if (horizontal.Length() > FacingMinSpeed)
            animator.FacingYaw = GameMath.YawFromDirection(new Vector3(horizontal.X, 0f, horizontal.Y));
    }

    /// <summary>Carried by a moving platform, then pushed out of anything that moved into us.</summary>
    private void RideGround(Entity player, PlayerComponent pc, TransformComponent transform)
    {
        if (pc.Grounded && context.Contains(pc.GroundEntityId) &&
            context.GetById(pc.GroundEntityId).TryGetComponent<ColliderComponent>(out var ground))
        {
            if (ground.Enabled)
                transform.Translation += ground.FrameDelta;
            else
                pc.Grounded = false;
        }

        var correction = Collision.Depenetrate(context, World.PlayerBox(player), player.Id);
        if (correction == Vector3.Zero)
            return;

        transform.Translation += correction;
        if (correction.Y > 0f && pc.Velocity.Y < 0f || correction.Y < 0f && pc.Velocity.Y > 0f)
            pc.Velocity = pc.Velocity with { Y = 0f };
    }

    private static void UpdateTimers(PlayerComponent pc, in GameInput input, PlayerTuningComponent tuning, float dt)
    {
        if (!pc.Grounded)
            pc.TimeSinceGrounded += dt;
        pc.TimeSinceJumpPressed = input.JumpPressed ? 0f : pc.TimeSinceJumpPressed + dt;

        if (pc.SpinTime > 0f)
        {
            pc.SpinTime -= dt;
            if (pc.SpinTime <= 0f)
                pc.SpinCooldownTime = tuning.SpinCooldown;
        }
        else
        {
            pc.SpinCooldownTime = MathF.Max(0f, pc.SpinCooldownTime - dt);
        }
    }

    private static Vector3 WishDirection(Vector2 move, float cameraYaw)
    {
        if (move == Vector2.Zero)
            return Vector3.Zero;
        var yaw = GameMath.YawRotation(cameraYaw);
        var forward = Vector3.Transform(-Vector3.UnitZ, yaw);
        var right = Vector3.Transform(Vector3.UnitX, yaw);
        return right * move.X + forward * move.Y;
    }

    private static void RunVelocity(PlayerComponent pc, Vector3 wish, PlayerTuningComponent tuning, float dt)
    {
        var current = new Vector2(pc.Velocity.X, pc.Velocity.Z);
        var target = new Vector2(wish.X, wish.Z) * tuning.MaxRunSpeed;
        var accelerating = target != Vector2.Zero;
        var rate = pc.Grounded
            ? accelerating ? tuning.GroundAcceleration : tuning.GroundDeceleration
            : accelerating ? tuning.AirAcceleration : tuning.AirDeceleration;
        var next = GameMath.MoveTowards(current, target, rate * dt);
        pc.Velocity = new Vector3(next.X, pc.Velocity.Y, next.Y);
    }

    private void Jump(PlayerComponent pc, in GameInput input, PlayerTuningComponent tuning, ProceduralAnimatorComponent animator)
    {
        var canGroundJump = !pc.JumpConsumed && (pc.Grounded || pc.TimeSinceGrounded <= tuning.CoyoteTime);
        if (pc.TimeSinceJumpPressed <= tuning.JumpBufferTime && canGroundJump)
        {
            pc.Velocity = pc.Velocity with { Y = tuning.JumpSpeed };
            pc.JumpConsumed = true;
            pc.JumpCutApplied = false;
            pc.Grounded = false;
            pc.TimeSinceJumpPressed = float.MaxValue;
            animator.Fire(AnimationEvent.Jump);
            audio.PlaySfx(Sfx.Jump, 0.6f);
        }
        else if (input.JumpPressed && !canGroundJump && pc.AirJumpsUsed < tuning.MaxAirJumps)
        {
            pc.Velocity = pc.Velocity with { Y = tuning.AirJumpSpeed };
            pc.AirJumpsUsed++;
            pc.JumpCutApplied = false;
            pc.TimeSinceJumpPressed = float.MaxValue;
            animator.Fire(AnimationEvent.AirJump);
            audio.PlaySfx(Sfx.DoubleJump, 0.6f);
        }

        if (!input.JumpHeld && pc.Velocity.Y > 0f && !pc.JumpCutApplied)
        {
            pc.Velocity = pc.Velocity with { Y = pc.Velocity.Y * tuning.JumpCutMultiplier };
            pc.JumpCutApplied = true;
        }
    }

    private void Spin(PlayerComponent pc, in GameInput input, PlayerTuningComponent tuning, ProceduralAnimatorComponent animator)
    {
        if (!input.SpinPressed || pc.IsSpinning || pc.SpinCooldownTime > 0f)
            return;

        pc.SpinTime = tuning.SpinDuration;
        animator.Fire(AnimationEvent.Spin);
        audio.PlaySfx(Sfx.Spin, 0.5f);
    }

    private static void Gravity(PlayerComponent pc, PlayerTuningComponent tuning, float dt)
    {
        var falling = pc.Velocity.Y < 0f;
        var gravity = tuning.Gravity * (falling ? tuning.FallGravityMultiplier : 1f);
        if (falling && pc.IsSpinning)
            gravity *= tuning.SpinFallGravityScale;
        pc.Velocity = pc.Velocity with { Y = MathF.Max(pc.Velocity.Y - gravity * dt, -tuning.MaxFallSpeed) };
    }

    private void Move(
        Entity player, PlayerComponent pc, TransformComponent transform, PlayerTuningComponent tuning,
        GameStateComponent state, GameSettingsComponent settings, ProceduralAnimatorComponent animator,
        in GameInput input, float dt)
    {
        var wasGrounded = pc.Grounded;
        var velocity = pc.Velocity;
        var box = World.PlayerBox(player);

        var dy = velocity.Y * dt;
        var allowedY = Collision.SweepAxis(context, box, 1, dy, player.Id, out var hitY);
        transform.Translation += new Vector3(0f, allowedY, 0f);
        box = box.Translate(new Vector3(0f, allowedY, 0f));
        pc.Grounded = false;

        if (hitY != null && dy < 0f)
        {
            if (hitY.HasComponent<CrateComponent>() && -velocity.Y >= tuning.CrateStompMinSpeed)
            {
                GameRules.BreakCrate(context, hitY, state, settings, audio);
                velocity.Y = Bounce(pc, tuning, input);
            }
            else
            {
                if (!wasGrounded)
                {
                    var impact = -velocity.Y / tuning.HardLandingSpeed;
                    animator.LandImpact = impact;
                    animator.Fire(AnimationEvent.Land);
                    if (impact > LandSoundImpact)
                        audio.PlaySfx(Sfx.Land, System.Math.Clamp(impact, 0.2f, 0.7f));
                }

                pc.Grounded = true;
                pc.GroundEntityId = hitY.Id;
                pc.TimeSinceGrounded = 0f;
                pc.JumpConsumed = false;
                pc.AirJumpsUsed = 0;
                velocity.Y = 0f;
            }
        }
        else if (hitY != null && dy > 0f)
        {
            if (hitY.HasComponent<CrateComponent>())
                GameRules.BreakCrate(context, hitY, state, settings, audio);
            velocity.Y = 0f;
        }

        foreach (var axis in (ReadOnlySpan<int>)[0, 2])
        {
            var delta = GameMath.Get(velocity, axis) * dt;
            var allowed = Collision.SweepAxis(context, box, axis, delta, player.Id, out _);
            if (MathF.Abs(allowed - delta) > BlockedVelocityEpsilon)
                velocity = GameMath.With(velocity, axis, 0f);
            var step = GameMath.With(Vector3.Zero, axis, allowed);
            transform.Translation += step;
            box = box.Translate(step);
        }

        pc.Velocity = velocity;
    }

    /// <summary>Bounce off a stomped enemy or crate; holding jump bounces higher (and is never cut short).</summary>
    public static float Bounce(PlayerComponent pc, PlayerTuningComponent tuning, in GameInput input)
    {
        pc.JumpCutApplied = true;
        pc.JumpConsumed = true;
        pc.AirJumpsUsed = 0;
        return input.JumpHeld ? tuning.StompBounceHeldSpeed : tuning.StompBounceSpeed;
    }
}
