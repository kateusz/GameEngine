namespace IslandDash;

/// <summary>Turns an animator's parameters and events into a pose. Implementations own the animator's driver-state fields.</summary>
public interface IAnimationDriver
{
    Pose Evaluate(ProceduralAnimatorComponent animator, float dt);
}

/// <summary>Driver lookup. Replace an entry (e.g. with a skeletal or keyframe driver) to change how a kind animates.</summary>
public static class AnimationDrivers
{
    private static readonly Dictionary<AnimationDriverKind, IAnimationDriver> Drivers = new()
    {
        [AnimationDriverKind.Player] = new PlayerDriver(),
        [AnimationDriverKind.Collectible] = new CollectibleDriver(),
        [AnimationDriverKind.Enemy] = new EnemyDriver(),
        [AnimationDriverKind.Platform] = new PlatformDriver(),
        [AnimationDriverKind.Pop] = new PopDriver()
    };

    public static IAnimationDriver For(AnimationDriverKind kind) => Drivers[kind];
}

/// <summary>Shared building blocks for the procedural drivers.</summary>
public static class Procedural
{
    /// <summary>Damped spring toward <paramref name="target"/> (critically damped-ish for the default constants).</summary>
    public static void Spring(ref float value, ref float velocity, float target, float stiffness, float damping, float dt)
    {
        velocity += (-(value - target) * stiffness - velocity * damping) * dt;
        value += velocity * dt;
    }

    /// <summary>Volume-preserving squash (negative) / stretch (positive) scale.</summary>
    public static Vector3 SquashStretch(float amount)
    {
        var y = MathF.Max(0.2f, 1f + amount);
        var xz = 1f / MathF.Sqrt(y);
        return new Vector3(xz, y, xz);
    }

    public static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        t = System.Math.Clamp(t, 0f, 1f) - 1f;
        return 1f + c3 * t * t * t + c1 * t * t;
    }

    public static float EaseOutCubic(float t)
    {
        t = 1f - System.Math.Clamp(t, 0f, 1f);
        return 1f - t * t * t;
    }
}

/// <summary>
/// Player: smooth turn toward the movement direction, forward/turn lean, run bob and waddle,
/// squash &amp; stretch on jump/landing, whole-body spin attack, death poof and respawn pop-in.
/// </summary>
public sealed class PlayerDriver : IAnimationDriver
{
    private const float TurnSharpness = 14f;
    private const float MaxForwardLean = 0.2f;
    private const float TurnLeanFactor = 0.05f;
    private const float MaxTurnLean = 0.3f;
    private const float LeanSharpness = 10f;
    private const float StrideRate = 13f;
    private const float RunBobHeight = 0.09f;
    private const float RunWaddle = 0.07f;
    private const float SquashStiffness = 260f;
    private const float SquashDamping = 16f;
    private const float JumpStretch = 0.32f;
    private const float AirJumpStretch = 0.22f;
    private const float MaxLandSquash = 0.42f;
    private const float AirStretchPerSpeed = 0.012f;
    private const float SpinRate = 26f;
    private const float DeathSpinRate = 18f;
    private const float DeathDuration = 0.9f;
    private const float RespawnDuration = 0.45f;
    private const float BlinkRate = 14f;

    public Pose Evaluate(ProceduralAnimatorComponent a, float dt)
    {
        a.Time += dt;
        a.SequenceTime += dt;
        HandleEvents(a);

        var previousYaw = a.Yaw;
        a.Yaw = GameMath.DampAngle(a.Yaw, a.FacingYaw, TurnSharpness, dt);
        var turnRate = dt > 0f ? GameMath.DeltaAngle(previousYaw, a.Yaw) / dt : 0f;

        var running = a.Grounded ? a.Speed01 : 0f;
        a.Lean = GameMath.Damp(a.Lean, a.Speed01 * MaxForwardLean, LeanSharpness, dt);
        a.Roll = GameMath.Damp(a.Roll, System.Math.Clamp(-turnRate * TurnLeanFactor * a.Speed01, -MaxTurnLean, MaxTurnLean), LeanSharpness, dt);
        a.Stride += dt * StrideRate * (0.35f + running);

        var squashTarget = a.Grounded ? 0f : System.Math.Clamp(a.VerticalSpeed * AirStretchPerSpeed, -0.12f, 0.2f);
        var squash = a.Squash;
        var squashVelocity = a.SquashVelocity;
        Procedural.Spring(ref squash, ref squashVelocity, squashTarget, SquashStiffness, SquashDamping, dt);
        a.Squash = squash;
        a.SquashVelocity = squashVelocity;

        if (a.Spinning)
            a.SpinAngle += SpinRate * dt;
        else
            a.SpinAngle = GameMath.DampAngle(a.SpinAngle, 0f, 20f, dt);

        var bob = MathF.Abs(MathF.Sin(a.Stride)) * RunBobHeight * running;
        var waddle = MathF.Sin(a.Stride) * RunWaddle * running;

        var rotation = Quaternion.CreateFromYawPitchRoll(a.Yaw + a.SpinAngle, a.Lean, a.Roll + waddle);
        var pose = Pose.At(rotation);
        pose.Offset = new Vector3(0f, bob, 0f);
        pose.Scale = Procedural.SquashStretch(a.Squash + (a.Spinning ? 0.06f : 0f));

        ApplySequence(a, ref pose, dt);
        a.Hidden = a.Blink && MathF.Sin(a.Time * BlinkRate) < -0.2f;
        return pose;
    }

    private static void HandleEvents(ProceduralAnimatorComponent a)
    {
        if (a.Consume(AnimationEvent.Jump))
            a.SquashVelocity += JumpStretch * 20f;
        if (a.Consume(AnimationEvent.AirJump))
            a.SquashVelocity += AirJumpStretch * 20f;
        if (a.Consume(AnimationEvent.Land))
        {
            a.Squash = -MaxLandSquash * System.Math.Clamp(a.LandImpact, 0.15f, 1f);
            a.SquashVelocity = 0f;
        }
        if (a.Consume(AnimationEvent.Die))
            a.StartSequence(AnimationEvent.Die);
        if (a.Consume(AnimationEvent.Respawn))
        {
            a.StartSequence(AnimationEvent.Respawn);
            a.Yaw = a.FacingYaw;
        }
        a.PendingEvents = AnimationEvent.None;
    }

    private static void ApplySequence(ProceduralAnimatorComponent a, ref Pose pose, float dt)
    {
        switch (a.Sequence)
        {
            case AnimationEvent.Die:
            {
                var t = a.SequenceTime / DeathDuration;
                a.SpinAngle += DeathSpinRate * dt;
                pose.Rotation = Quaternion.CreateFromYawPitchRoll(a.Yaw + a.SpinAngle, 0f, 0f);
                pose.Offset += new Vector3(0f, Procedural.EaseOutCubic(t) * 0.8f, 0f);
                var s = MathF.Max(0f, 1f - t * t);
                pose.Scale = new Vector3(s * (1f + 0.4f * t), s, s * (1f + 0.4f * t));
                break;
            }
            case AnimationEvent.Respawn:
            {
                var t = a.SequenceTime / RespawnDuration;
                pose.Scale *= MathF.Max(0.01f, Procedural.EaseOutBack(t));
                if (t >= 1f)
                    a.Sequence = AnimationEvent.None;
                break;
            }
        }
    }
}

/// <summary>Crystals, hearts and the goal: constant spin with a sine bob. Pop event hands over to the pop sequence.</summary>
public sealed class CollectibleDriver : IAnimationDriver
{
    private const float BobRate = 2.6f;

    public Pose Evaluate(ProceduralAnimatorComponent a, float dt)
    {
        a.Time += dt;
        if (a.Consume(AnimationEvent.Pop))
            a.StartSequence(AnimationEvent.Pop);

        a.SpinAngle += a.SpinSpeed * dt;
        var pose = Pose.At(GameMath.YawRotation(a.RestYaw + a.SpinAngle));
        pose.Offset = new Vector3(0f, (MathF.Sin(a.Time * BobRate + a.Phase) * 0.5f + 0.5f) * a.BobHeight, 0f);

        if (a.Sequence == AnimationEvent.Pop)
            PopDriver.ApplyPop(a, ref pose, dt);
        return pose;
    }
}

/// <summary>Crates: squash bounce when hit, then the reward pops out (see <see cref="ApplyPop"/>).</summary>
public sealed class PopDriver : IAnimationDriver
{
    private const float PopDuration = 0.65f;
    private const float PopRise = 1.5f;
    private const float PopSpinRate = 14f;
    private const float SquashDuration = 0.25f;

    public Pose Evaluate(ProceduralAnimatorComponent a, float dt)
    {
        a.Time += dt;
        if (a.Consume(AnimationEvent.Pop))
            a.StartSequence(AnimationEvent.Pop);
        if (a.Consume(AnimationEvent.Squash))
            a.StartSequence(AnimationEvent.Squash);

        var pose = Pose.At(GameMath.YawRotation(a.RestYaw));
        if (a.Sequence == AnimationEvent.Squash)
        {
            a.SequenceTime += dt;
            var t = a.SequenceTime / SquashDuration;
            pose.Scale = Procedural.SquashStretch(-0.35f * MathF.Sin(MathF.PI * System.Math.Clamp(t, 0f, 1f)));
            if (t >= 1f)
                a.Sequence = AnimationEvent.None;
        }
        else if (a.Sequence == AnimationEvent.Pop)
        {
            ApplyPop(a, ref pose, dt);
        }

        return pose;
    }

    public static void ApplyPop(ProceduralAnimatorComponent a, ref Pose pose, float dt)
    {
        a.SequenceTime += dt;
        var t = a.SequenceTime / PopDuration;
        a.SpinAngle += PopSpinRate * dt;
        pose.Rotation = GameMath.YawRotation(a.RestYaw + a.SpinAngle);
        pose.Offset += new Vector3(0f, Procedural.EaseOutCubic(t) * PopRise, 0f);
        var grow = t < 0.25f ? 1f + t * 1.6f : 1.4f * (1f - (t - 0.25f) / 0.75f);
        pose.Scale *= MathF.Max(0f, grow);
        if (t >= 1f)
        {
            a.Hidden = true;
            a.Sequence = AnimationEvent.None;
        }
    }
}

/// <summary>Enemies: turn toward walking direction, waddle roll, step bob, sinusoidal side sway; stomp flattens, spin hit tumbles.</summary>
public sealed class EnemyDriver : IAnimationDriver
{
    private const float TurnSharpness = 8f;
    private const float WaddleRate = 9f;
    private const float WaddleRoll = 0.16f;
    private const float StepBob = 0.07f;
    private const float SwayRate = 1.4f;
    private const float SwayDistance = 0.18f;
    private const float DefeatDuration = 0.9f;

    public Pose Evaluate(ProceduralAnimatorComponent a, float dt)
    {
        a.Time += dt;
        if (a.Consume(AnimationEvent.Squash))
            a.StartSequence(AnimationEvent.Squash);
        if (a.Consume(AnimationEvent.Die))
            a.StartSequence(AnimationEvent.Die);
        if (a.Consume(AnimationEvent.Respawn))
        {
            a.Sequence = AnimationEvent.None;
            a.Hidden = false;
        }

        a.Yaw = GameMath.DampAngle(a.Yaw, a.FacingYaw, TurnSharpness, dt);
        var walk = 0.35f + a.Speed01;
        var cycle = a.Time * WaddleRate * walk + a.Phase;
        var sway = MathF.Sin(a.Time * SwayRate + a.Phase) * SwayDistance;
        var right = Vector3.Transform(Vector3.UnitX, GameMath.YawRotation(a.Yaw));

        var pose = Pose.At(Quaternion.CreateFromYawPitchRoll(a.Yaw, 0.08f * a.Speed01, MathF.Sin(cycle) * WaddleRoll));
        pose.Offset = right * sway + new Vector3(0f, MathF.Abs(MathF.Sin(cycle)) * StepBob, 0f);

        if (a.Sequence == AnimationEvent.Squash || a.Sequence == AnimationEvent.Die)
        {
            a.SequenceTime += dt;
            var t = a.SequenceTime / DefeatDuration;
            if (a.Sequence == AnimationEvent.Squash)
            {
                pose.Offset = Vector3.Zero;
                pose.Rotation = GameMath.YawRotation(a.Yaw);
                pose.Scale = new Vector3(1.5f, MathF.Max(0.12f, 1f - t * 6f), 1.5f) * (t > 0.6f ? MathF.Max(0f, 1f - (t - 0.6f) / 0.4f) : 1f);
            }
            else
            {
                a.SpinAngle += 16f * dt;
                pose.Rotation = Quaternion.CreateFromYawPitchRoll(a.Yaw + a.SpinAngle, a.SpinAngle * 0.7f, 0f);
                pose.Scale = new Vector3(MathF.Max(0f, 1f - t * t));
            }

            if (t >= 1f)
            {
                a.Hidden = true;
                a.Sequence = AnimationEvent.None;
            }
        }

        return pose;
    }
}

/// <summary>Platforms and crushers: jitter while <see cref="ProceduralAnimatorComponent.Shake"/> &gt; 0, flicker on blink, pop-in on respawn.</summary>
public sealed class PlatformDriver : IAnimationDriver
{
    private const float ShakeDistance = 0.07f;
    private const float ShakeRate = 55f;
    private const float BlinkRate = 18f;
    private const float RespawnDuration = 0.4f;

    public Pose Evaluate(ProceduralAnimatorComponent a, float dt)
    {
        a.Time += dt;
        if (a.Consume(AnimationEvent.Respawn))
            a.StartSequence(AnimationEvent.Respawn);

        var pose = Pose.At(GameMath.YawRotation(a.RestYaw));
        if (a.Shake > 0f)
        {
            var t = a.Time * ShakeRate;
            pose.Offset = new Vector3(MathF.Sin(t * 1.3f), 0.3f * MathF.Sin(t * 0.7f), MathF.Cos(t)) * (ShakeDistance * a.Shake);
        }

        if (a.Sequence == AnimationEvent.Respawn)
        {
            a.SequenceTime += dt;
            var t = a.SequenceTime / RespawnDuration;
            pose.Scale = new Vector3(MathF.Max(0.01f, Procedural.EaseOutBack(t)));
            if (t >= 1f)
                a.Sequence = AnimationEvent.None;
        }

        a.Hidden = a.Blink && MathF.Sin(a.Time * BlinkRate) < 0f;
        return pose;
    }
}
