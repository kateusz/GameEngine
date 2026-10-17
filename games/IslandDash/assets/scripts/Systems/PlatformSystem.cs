using ECS;
using ECS.Systems;
using SceneComponents;
using Scripting;

namespace IslandDash;

/// <summary>Kinematic level pieces: moving, vanishing and falling platforms, crushers and timed spikes. Runs before the player so riders get this frame's motion.</summary>
[Register(typeof(IGameSystem))]
public class PlatformSystem(Context context) : IGameSystem
{
    private const float FallGravity = 22f;
    private const float FallDistance = 30f;
    private const float SpikeRetractDepth = 0.45f;
    private const float SpikeMoveSpeed = 6f;
    private const float WaypointArrival = 0.01f;

    // Crusher cycle as fractions of its period.
    private const float CrusherHoldUp = 0.45f;
    private const float CrusherShake = 0.15f;
    private const float CrusherSlam = 0.06f;
    private const float CrusherHoldDown = 0.12f;

    public int Priority => SystemOrder.Platforms;

    public void OnInit() { }

    public void OnShutdown() { }

    public void OnUpdate(TimeSpan deltaTime)
    {
        if (World.State(context) is not { } state || state.Phase is GamePhase.Paused or GamePhase.Title)
        {
            ClearDeltas();
            return;
        }

        var dt = SystemOrder.ClampDelta(deltaTime);
        var groundId = World.Player(context)?.GetComponent<PlayerComponent>() is { Grounded: true } pc ? pc.GroundEntityId : -1;

        UpdateMovers(dt);
        UpdateVanishing(dt);
        UpdateFallers(dt, groundId);
        UpdateCrushers(dt);
        UpdateSpikes(dt);
    }

    private void ClearDeltas()
    {
        foreach (var (_, collider) in context.View<ColliderComponent>())
            collider.FrameDelta = Vector3.Zero;
    }

    private void UpdateMovers(float dt)
    {
        foreach (var (entity, mover) in context.View<MovingPlatformComponent>())
        {
            mover.Time += dt;
            var target = mover.Waypoints.Count > 1
                ? mover.Origin + PingPong(mover.Waypoints, mover.Time * mover.Speed)
                : mover.Origin + mover.Amplitude * MathF.Sin(MathF.Tau * (mover.Time / mover.Period + mover.Phase));
            MoveTo(entity, target);
        }
    }

    /// <summary>Point at <paramref name="distance"/> along the polyline, bouncing back at the ends.</summary>
    private static Vector3 PingPong(List<Vector3> points, float distance)
    {
        var length = 0f;
        for (var i = 1; i < points.Count; i++)
            length += Vector3.Distance(points[i - 1], points[i]);
        if (length < WaypointArrival)
            return points[0];

        var d = distance % (2f * length);
        if (d > length)
            d = 2f * length - d;
        for (var i = 1; i < points.Count; i++)
        {
            var segment = Vector3.Distance(points[i - 1], points[i]);
            if (d <= segment)
                return Vector3.Lerp(points[i - 1], points[i], segment > 0f ? d / segment : 0f);
            d -= segment;
        }

        return points[^1];
    }

    private void UpdateVanishing(float dt)
    {
        foreach (var (entity, vanish) in context.View<VanishingPlatformComponent>())
        {
            vanish.Time += dt;
            var cycle = vanish.OnTime + vanish.OffTime;
            var t = ((vanish.Time + vanish.Phase * cycle) % cycle + cycle) % cycle;
            var solid = t < vanish.OnTime;

            var collider = entity.GetComponent<ColliderComponent>();
            collider.FrameDelta = Vector3.Zero;
            if (entity.TryGetComponent<ProceduralAnimatorComponent>(out var animator))
            {
                if (solid && !collider.Enabled)
                    animator.Fire(AnimationEvent.Respawn);
                animator.Blink = solid && t > vanish.OnTime - vanish.WarningTime;
                animator.Hidden = !solid;
            }
            collider.Enabled = solid;
        }
    }

    private void UpdateFallers(float dt, int playerGroundId)
    {
        foreach (var (entity, faller) in context.View<FallingPlatformComponent>())
        {
            var collider = entity.GetComponent<ColliderComponent>();
            var transform = entity.GetComponent<TransformComponent>();
            entity.TryGetComponent<ProceduralAnimatorComponent>(out var animator);
            collider.FrameDelta = Vector3.Zero;
            faller.Timer += dt;

            switch (faller.State)
            {
                case FallerState.Idle when playerGroundId == entity.Id:
                    faller.State = FallerState.Shaking;
                    faller.Timer = 0f;
                    break;
                case FallerState.Shaking:
                    if (animator != null)
                        animator.Shake = faller.Timer / faller.ShakeTime;
                    if (faller.Timer >= faller.ShakeTime)
                    {
                        faller.State = FallerState.Falling;
                        faller.Timer = 0f;
                        faller.FallSpeed = 0f;
                        if (animator != null)
                            animator.Shake = 0f;
                    }
                    break;
                case FallerState.Falling:
                    faller.FallSpeed += FallGravity * dt;
                    MoveTo(entity, transform.Translation - new Vector3(0f, faller.FallSpeed * dt, 0f));
                    if (faller.Origin.Y - transform.Translation.Y > FallDistance)
                    {
                        faller.State = FallerState.Gone;
                        faller.Timer = 0f;
                        collider.Enabled = false;
                        collider.FrameDelta = Vector3.Zero;
                        if (animator != null)
                            animator.Hidden = true;
                    }
                    break;
                case FallerState.Gone when faller.Timer >= faller.RespawnTime:
                    faller.State = FallerState.Idle;
                    transform.Translation = faller.Origin;
                    collider.Enabled = true;
                    if (animator != null)
                    {
                        animator.Hidden = false;
                        animator.Fire(AnimationEvent.Respawn);
                    }
                    break;
            }
        }
    }

    private void UpdateCrushers(float dt)
    {
        foreach (var (entity, crusher) in context.View<CrusherComponent>())
        {
            crusher.Time += dt;
            var t = ((crusher.Time / crusher.Period + crusher.Phase) % 1f + 1f) % 1f;
            float lift;
            var shake = 0f;
            if (t < CrusherHoldUp)
                lift = 1f;
            else if (t < CrusherHoldUp + CrusherShake)
            {
                lift = 1f;
                shake = 1f;
            }
            else if (t < CrusherHoldUp + CrusherShake + CrusherSlam)
            {
                var s = (t - CrusherHoldUp - CrusherShake) / CrusherSlam;
                lift = 1f - s * s;
            }
            else if (t < CrusherHoldUp + CrusherShake + CrusherSlam + CrusherHoldDown)
                lift = 0f;
            else
            {
                const float riseStart = CrusherHoldUp + CrusherShake + CrusherSlam + CrusherHoldDown;
                lift = (t - riseStart) / (1f - riseStart);
            }

            MoveTo(entity, crusher.Bottom + new Vector3(0f, crusher.Height * lift, 0f));
            if (entity.TryGetComponent<ProceduralAnimatorComponent>(out var animator))
                animator.Shake = shake;
        }
    }

    private void UpdateSpikes(float dt)
    {
        foreach (var (entity, spikes) in context.View<TimedSpikesComponent>())
        {
            spikes.Time += dt;
            var cycle = spikes.UpTime + spikes.DownTime;
            var t = ((spikes.Time + spikes.Phase * cycle) % cycle + cycle) % cycle;
            var up = t < spikes.UpTime;

            spikes.Depth = MoveTowards(spikes.Depth, up ? 0f : SpikeRetractDepth, SpikeMoveSpeed * dt);
            var transform = entity.GetComponent<TransformComponent>();
            transform.Translation = transform.Translation with { Y = spikes.FloorY - spikes.Depth };
            entity.GetComponent<HazardComponent>().Active = spikes.Depth < SpikeRetractDepth * 0.5f;
        }
    }

    private static float MoveTowards(float current, float target, float maxDelta) =>
        MathF.Abs(target - current) <= maxDelta ? target : current + MathF.Sign(target - current) * maxDelta;

    private static void MoveTo(Entity entity, Vector3 target)
    {
        var transform = entity.GetComponent<TransformComponent>();
        if (entity.TryGetComponent<ColliderComponent>(out var collider))
            collider.FrameDelta = target - transform.Translation;
        transform.Translation = target;
    }
}
