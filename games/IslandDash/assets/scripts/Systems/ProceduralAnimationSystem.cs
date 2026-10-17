using ECS;
using ECS.Systems;
using SceneComponents;
using Scripting;

namespace IslandDash;

/// <summary>Evaluates every <see cref="ProceduralAnimatorComponent"/> and writes the resulting pose to its visual entity.</summary>
[Register(typeof(IGameSystem))]
public class ProceduralAnimationSystem(Context context) : IGameSystem
{
    public int Priority => SystemOrder.Animation;

    public void OnInit() { }

    public void OnShutdown() { }

    public void OnUpdate(TimeSpan deltaTime)
    {
        var dt = SystemOrder.ClampDelta(deltaTime);
        if (World.State(context) is { Phase: GamePhase.Paused })
            dt = 0f;

        foreach (var (_, animator, owner) in context.View<ProceduralAnimatorComponent, TransformComponent>())
        {
            if (!context.Contains(animator.VisualEntityId))
                continue;

            var visual = context.GetById(animator.VisualEntityId);
            if (!visual.TryGetComponent<TransformComponent>(out var transform))
                continue;

            var pose = AnimationDrivers.For(animator.Driver).Evaluate(animator, dt);
            Apply(animator, owner.Translation, pose, transform);
        }
    }

    public static void Apply(ProceduralAnimatorComponent animator, Vector3 ownerPosition, in Pose pose, TransformComponent visual)
    {
        var scale = animator.RestScale * pose.Scale;
        visual.Translation = ownerPosition + pose.Offset + Vector3.Transform(animator.RestOffset * pose.Scale, pose.Rotation);
        visual.Rotation = GameMath.ToEngineEuler(pose.Rotation);
        visual.Scale = scale;
        visual.Visible = !animator.Hidden && scale.X > 1e-4f && scale.Y > 1e-4f;
    }
}
