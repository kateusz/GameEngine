using ECS;
using ECS.Systems;
using SceneComponents;
using Scripting;

namespace IslandDash;

/// <summary>Patrollers walk their waypoints; chasers run at the player inside a small radius and return home when it leaves.</summary>
// ponytail: enemies keep their spawn height and ignore walls (levels place them on flat ground). Upgrade path:
// reuse Collision.SweepAxis + gravity like the player once enemies need to walk over uneven terrain.
[Register(typeof(IGameSystem))]
public class EnemySystem(Context context) : IGameSystem
{
    private const float ArriveDistance = 0.15f;
    private const float MaxChaseHeightDifference = 2.5f;
    private const float DefeatGravity = 25f;
    private const float DefeatDuration = 1.2f;

    public int Priority => SystemOrder.Enemies;

    public void OnInit() { }

    public void OnShutdown() { }

    public void OnUpdate(TimeSpan deltaTime)
    {
        if (World.State(context) is not { } state || state.Phase is GamePhase.Paused or GamePhase.Title)
            return;

        var dt = SystemOrder.ClampDelta(deltaTime);
        var player = World.Player(context);
        var playerPosition = player?.GetComponent<TransformComponent>().Translation;
        var playerActive = state.Phase == GamePhase.Playing;

        foreach (var (entity, enemy) in context.View<EnemyComponent>())
        {
            var transform = entity.GetComponent<TransformComponent>();
            var animator = entity.GetComponent<ProceduralAnimatorComponent>();

            if (!enemy.Alive)
            {
                Defeated(enemy, transform, dt);
                continue;
            }

            var position = transform.Translation;
            var chasing = enemy.Behavior == EnemyBehavior.Chase && playerActive &&
                          playerPosition is { } p && CanChase(enemy, position, p);
            var target = chasing ? ClampToLeash(enemy, playerPosition!.Value) : PatrolTarget(enemy, position);
            var speed = chasing ? enemy.ChaseSpeed : enemy.Speed;

            var toTarget = new Vector3(target.X - position.X, 0f, target.Z - position.Z);
            var distance = toTarget.Length();
            if (distance > ArriveDistance)
            {
                var step = toTarget / distance * MathF.Min(distance, speed * dt);
                transform.Translation = position + step;
                animator.FacingYaw = GameMath.YawFromDirection(toTarget);
                animator.Speed01 = System.Math.Clamp(speed / enemy.ChaseSpeed, 0f, 1f);
            }
            else
            {
                animator.Speed01 = 0f;
            }

            entity.GetComponent<ColliderComponent>().FrameDelta = transform.Translation - position;
        }
    }

    private static bool CanChase(EnemyComponent enemy, Vector3 position, Vector3 player) =>
        MathF.Abs(player.Y - position.Y) < MaxChaseHeightDifference &&
        Vector3.Distance(Flat(player), Flat(position)) < enemy.ChaseRadius &&
        Vector3.Distance(Flat(player), Flat(enemy.Home)) < enemy.Leash + enemy.ChaseRadius;

    private static Vector3 ClampToLeash(EnemyComponent enemy, Vector3 player)
    {
        var offset = Flat(player) - Flat(enemy.Home);
        if (offset.Length() > enemy.Leash)
            offset = Vector3.Normalize(offset) * enemy.Leash;
        return enemy.Home + offset;
    }

    private static Vector3 PatrolTarget(EnemyComponent enemy, Vector3 position)
    {
        if (enemy.Waypoints.Count < 2)
            return enemy.Waypoints.Count == 1 ? enemy.Waypoints[0] : enemy.Home;

        var target = enemy.Waypoints[enemy.TargetWaypoint];
        if (Vector3.Distance(Flat(position), Flat(target)) <= ArriveDistance)
        {
            enemy.TargetWaypoint = (enemy.TargetWaypoint + 1) % enemy.Waypoints.Count;
            target = enemy.Waypoints[enemy.TargetWaypoint];
        }

        return target;
    }

    private static void Defeated(EnemyComponent enemy, TransformComponent transform, float dt)
    {
        enemy.DefeatTime += dt;
        if (enemy.DefeatTime > DefeatDuration || enemy.KnockVelocity == Vector3.Zero)
            return;
        enemy.KnockVelocity -= new Vector3(0f, DefeatGravity * dt, 0f);
        transform.Translation += enemy.KnockVelocity * dt;
    }

    private static Vector3 Flat(Vector3 v) => v with { Y = 0f };

    /// <summary>Back to spawn, alive (called on player respawn so every attempt starts the same).</summary>
    public static void ResetAll(Context context)
    {
        foreach (var (entity, enemy) in context.View<EnemyComponent>())
        {
            enemy.Alive = true;
            enemy.DefeatTime = 0f;
            enemy.KnockVelocity = Vector3.Zero;
            enemy.TargetWaypoint = 0;
            entity.GetComponent<TransformComponent>().Translation = enemy.Home;
            entity.GetComponent<ColliderComponent>().Enabled = true;
            entity.GetComponent<ProceduralAnimatorComponent>().Fire(AnimationEvent.Respawn);
        }
    }
}
