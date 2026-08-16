using System.Numerics;
using Engine.Renderer;
using Engine.Renderer.Models;
using Engine.Scene.Skeletal;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class SkeletalPoseMathTests
{
    [Fact]
    public void Evaluate_NoClip_IsIdentityPalette()
    {
        var bones = TwoBoneChain();
        var palette = SkeletalPoseMath.CreateIdentityPalette();

        SkeletalPoseMath.Evaluate(bones, clip: null, time: 0f, palette);

        palette[0].ShouldBe(Matrix4x4.Identity);
        palette[1].ShouldBe(Matrix4x4.Identity);
    }

    [Fact]
    public void Evaluate_FirstKeyMatchingRest_IsIdentityPalette()
    {
        var bones = TwoBoneChain();
        var clip = new AnimationClip("bind", 1f,
        [
            new BoneChannel(
                1,
                [new VectorKey(0f, new Vector3(0, 1, 0))],
                [new RotationKey(0f, Quaternion.Identity)],
                [new VectorKey(0f, Vector3.One)])
        ]);
        var palette = SkeletalPoseMath.CreateIdentityPalette();

        SkeletalPoseMath.Evaluate(bones, clip, time: 0f, palette);

        AssertNearIdentity(palette[0]);
        AssertNearIdentity(palette[1]);
    }

    [Fact]
    public void Evaluate_FirstKeysDisagreeWithRest_AppliesTheFirstKey()
    {
        var bones = TwoBoneChain();
        var clip = new AnimationClip("mixamo-first-frame", 1f,
        [
            new BoneChannel(
                1,
                [new VectorKey(0f, new Vector3(0, 5, 0))],
                [new RotationKey(0f, Quaternion.Identity)],
                [new VectorKey(0f, Vector3.One)])
        ]);
        var palette = SkeletalPoseMath.CreateIdentityPalette();

        SkeletalPoseMath.Evaluate(bones, clip, time: 0f, palette);

        var joint = Vector3.Transform(new Vector3(0, 1, 0), palette[1]);
        joint.Y.ShouldBe(5f, 1e-4f);
    }

    [Fact]
    public void Evaluate_RotatingChild_DoesNotMoveTheJoint()
    {
        var bones = TwoBoneChain();
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2f);
        var clip = new AnimationClip("spin", 1f,
        [
            new BoneChannel(
                1,
                [new VectorKey(0f, new Vector3(0, 1, 0)), new VectorKey(1f, new Vector3(0, 1, 0))],
                [new RotationKey(0f, Quaternion.Identity), new RotationKey(1f, rotation)],
                [new VectorKey(0f, Vector3.One), new VectorKey(1f, Vector3.One)])
        ]);
        var palette = SkeletalPoseMath.CreateIdentityPalette();
        SkeletalPoseMath.Evaluate(bones, clip, time: 1f, palette);

        var joint = new Vector3(0, 1, 0);
        var skinned = Vector3.Transform(joint, palette[1]);
        skinned.X.ShouldBe(0f, 1e-5f);
        skinned.Y.ShouldBe(1f, 1e-5f);
        skinned.Z.ShouldBe(0f, 1e-5f);

        var tip = Vector3.Transform(new Vector3(0, 2, 0), palette[1]);
        tip.X.ShouldBe(-1f, 1e-4f);
        tip.Y.ShouldBe(1f, 1e-4f);
    }

    [Fact]
    public void Sample_LerpsTranslationBetweenKeys()
    {
        var bones = new[] { new SkeletonBone("root", -1, Matrix4x4.Identity) };
        var clip = new AnimationClip("slide", 1f,
        [
            new BoneChannel(
                0,
                [new VectorKey(0f, Vector3.Zero), new VectorKey(1f, new Vector3(10, 0, 0))],
                [new RotationKey(0f, Quaternion.Identity)],
                [new VectorKey(0f, Vector3.One)])
        ]);
        var palette = SkeletalPoseMath.CreateIdentityPalette();
        SkeletalPoseMath.Evaluate(bones, clip, time: 0.5f, palette);

        var origin = Vector3.Transform(Vector3.Zero, palette[0]);
        origin.X.ShouldBe(5f, 1e-4f);
    }

    [Fact]
    public void AdvanceTime_Loop_WrapsPastDuration()
    {
        SkeletalPoseMath.AdvanceTime(0.9f, 0.3f, speed: 1f, duration: 1f, loop: true)
            .ShouldBe(0.2f, 1e-5f);
    }

    [Fact]
    public void AdvanceTime_NoLoop_ClampsToDuration()
    {
        SkeletalPoseMath.AdvanceTime(0.9f, 0.3f, speed: 1f, duration: 1f, loop: false)
            .ShouldBe(1f);
    }

    private static SkeletonBone[] TwoBoneChain() =>
    [
        new("root", -1, Matrix4x4.Identity),
        new("child", 0, Matrix4x4.CreateTranslation(0, -1, 0))
    ];

    private static void AssertNearIdentity(Matrix4x4 m)
    {
        m.M11.ShouldBe(1f, 1e-4f); m.M12.ShouldBe(0f, 1e-4f); m.M13.ShouldBe(0f, 1e-4f); m.M14.ShouldBe(0f, 1e-4f);
        m.M21.ShouldBe(0f, 1e-4f); m.M22.ShouldBe(1f, 1e-4f); m.M23.ShouldBe(0f, 1e-4f); m.M24.ShouldBe(0f, 1e-4f);
        m.M31.ShouldBe(0f, 1e-4f); m.M32.ShouldBe(0f, 1e-4f); m.M33.ShouldBe(1f, 1e-4f); m.M34.ShouldBe(0f, 1e-4f);
        m.M41.ShouldBe(0f, 1e-4f); m.M42.ShouldBe(0f, 1e-4f); m.M43.ShouldBe(0f, 1e-4f); m.M44.ShouldBe(1f, 1e-4f);
    }
}
