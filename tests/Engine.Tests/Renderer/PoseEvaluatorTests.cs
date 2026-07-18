using System.Numerics;
using Engine.Renderer.Animation;
using Shouldly;

namespace Engine.Tests.Renderer;

public class PoseEvaluatorTests
{
    [Fact]
    public void Evaluate_TranslatesRootBone_ProducesRootDelta()
    {
        var skeleton = new Skeleton([
            new BoneData("Root", -1, Matrix4x4.Identity)
        ]);

        var clip = new AnimationClip
        {
            Name = "Walk",
            DurationSeconds = 1f,
            Tracks =
            [
                new BoneTrack
                {
                    BoneIndex = 0,
                    Positions =
                    [
                        new VectorKey(0f, Vector3.Zero),
                        new VectorKey(1f, new Vector3(2f, 0f, 0f))
                    ],
                    Rotations = [new QuatKey(0f, Quaternion.Identity)],
                    Scales = [new VectorKey(0f, Vector3.One)]
                }
            ]
        };

        var atStart = PoseEvaluator.Evaluate(skeleton, clip, 0f, loop: false,
            computeRootDelta: false, previousRootGlobal: Matrix4x4.Identity, hasPreviousRoot: false);
        var atHalf = PoseEvaluator.Evaluate(skeleton, clip, 0.5f, loop: false,
            computeRootDelta: true, previousRootGlobal: atStart.RootGlobal, hasPreviousRoot: true);

        atHalf.RootDelta.X.ShouldBe(1f, 0.01);
        atHalf.RootDelta.Y.ShouldBe(0f, 0.01);
        atHalf.SkinMatrices.Length.ShouldBe(1);
    }

    [Fact]
    public void ResolveTime_Loop_WrapsWithoutNegative()
    {
        PoseEvaluator.ResolveTime(1.25f, 1f, loop: true).ShouldBe(0.25f, 0.001);
        PoseEvaluator.ResolveTime(-0.25f, 1f, loop: true).ShouldBe(0.75f, 0.001);
    }

    [Fact]
    public void CrossedLoopBoundary_DetectsWrap()
    {
        PoseEvaluator.CrossedLoopBoundary(0.9f, 1.1f, 1f, loop: true).ShouldBeTrue();
        PoseEvaluator.CrossedLoopBoundary(0.2f, 0.4f, 1f, loop: true).ShouldBeFalse();
    }

    [Fact]
    public void Evaluate_MissingClip_UsesBindPoseIdentityLocals()
    {
        var skeleton = new Skeleton([
            new BoneData("Root", -1, Matrix4x4.Identity),
            new BoneData("Child", 0, Matrix4x4.Identity)
        ]);

        var pose = PoseEvaluator.Evaluate(skeleton, clip: null, timeSeconds: 0f, loop: false,
            computeRootDelta: false, previousRootGlobal: Matrix4x4.Identity, hasPreviousRoot: false);

        pose.SkinMatrices.Length.ShouldBe(2);
        pose.SkinMatrices[0].ShouldBe(Matrix4x4.Identity);
    }
}

public class AnimatorComponentTests
{
    [Fact]
    public void Play_SetsClipAndPlaying()
    {
        var animator = new SceneComponents.Rendering.AnimatorComponent();
        animator.Play("Walk");

        animator.ClipName.ShouldBe("Walk");
        animator.IsPlaying.ShouldBeTrue();
        animator.Time.ShouldBe(0f);
    }

    [Fact]
    public void Stop_ClearsPlaybackState()
    {
        var animator = new SceneComponents.Rendering.AnimatorComponent
        {
            ClipName = "Walk",
            Time = 1.5f,
            IsPlaying = true,
            HasPose = true,
            SkinMatrices = [Matrix4x4.Identity]
        };

        animator.Stop();

        animator.IsPlaying.ShouldBeFalse();
        animator.Time.ShouldBe(0f);
        animator.HasPose.ShouldBeFalse();
        animator.SkinMatrices.ShouldBeNull();
    }

    [Fact]
    public void Clone_CopiesAuthoringFieldsOnly()
    {
        var original = new SceneComponents.Rendering.AnimatorComponent
        {
            ClipName = "Idle",
            Loop = false,
            Speed = 2f,
            ApplyRootMotion = true,
            IsPlaying = true
        };

        var clone = (SceneComponents.Rendering.AnimatorComponent)original.Clone();
        clone.ClipName.ShouldBe("Idle");
        clone.Loop.ShouldBeFalse();
        clone.Speed.ShouldBe(2f);
        clone.ApplyRootMotion.ShouldBeTrue();
        clone.SkinMatrices.ShouldBeNull();
    }
}
