using ECS;
using ECS.Systems;
using Engine.Core;
using Engine.Renderer;
using Engine.Renderer.Animation;
using SceneComponents;
using SceneComponents.Rendering;
using Serilog;

namespace Engine.Scene.Systems;

internal sealed class AnimationSystem(IContext context, IModelFactory modelFactory) : ISystem
{
    private static readonly ILogger Logger = Log.ForContext<AnimationSystem>();

    public int Priority => SystemPriorities.AnimationSystem;

    public void OnInit()
    {
        Logger.Debug("AnimationSystem initialized with priority {Priority}", Priority);
    }

    public void OnUpdate(TimeSpan deltaTime)
    {
        var dt = (float)deltaTime.TotalSeconds;

        foreach (var (entity, animator, modelRenderer) in
                 context.View<AnimatorComponent, ModelRendererComponent>())
        {
            if (!entity.TryGetComponent<TransformComponent>(out var transform))
                continue;

            if (string.IsNullOrWhiteSpace(modelRenderer.ModelPath))
            {
                ClearPose(animator);
                continue;
            }

            var model = modelFactory.Create(PathBuilder.Resolve(modelRenderer.ModelPath));
            if (model?.Skeleton == null || !model.HasSkeleton)
            {
                ClearPose(animator);
                continue;
            }

            var clip = model.FindClip(animator.ClipName);
            if (clip == null)
            {
                if (animator.IsPlaying && !string.IsNullOrWhiteSpace(animator.ClipName))
                {
                    Logger.Warning("Animation clip not found: {ClipName} in {ModelPath}",
                        animator.ClipName, modelRenderer.ModelPath);
                    animator.IsPlaying = false;
                }

                // Bind-pose draw: leave u_HasBones off (mesh verts already in bind pose).
                ClearPose(animator);
                continue;
            }

            // Stopped at time 0 → bind pose (no GPU skinning).
            if (!animator.IsPlaying && animator.Time <= 0f)
            {
                ClearPose(animator);
                continue;
            }

            var previousTime = animator.Time;
            if (animator.IsPlaying)
                animator.Time += dt * animator.Speed;

            var looped = PoseEvaluator.CrossedLoopBoundary(
                previousTime, animator.Time, clip.DurationSeconds, animator.Loop);

            if (looped)
                animator.HasPreviousRoot = false;

            var pose = PoseEvaluator.Evaluate(
                model.Skeleton,
                clip,
                animator.Time,
                animator.Loop,
                animator.ApplyRootMotion,
                animator.PreviousRootGlobal,
                animator.HasPreviousRoot);

            animator.SkinMatrices = pose.SkinMatrices;
            animator.HasPose = true;
            animator.PreviousRootGlobal = pose.RootGlobal;
            animator.HasPreviousRoot = true;

            if (animator.ApplyRootMotion && pose.RootDelta != default)
                transform.Translation += pose.RootDelta;
        }
    }

    public void OnShutdown() { }

    private static void ClearPose(AnimatorComponent animator)
    {
        animator.HasPose = false;
        animator.SkinMatrices = null;
        animator.HasPreviousRoot = false;
    }
}
