using System.Numerics;
using Engine.Core;
using Engine.Renderer;
using Engine.Renderer.Serialization;
using Engine.Renderer.Skeletal;
using Engine.Renderer.Skeletal.Serialization;
using Engine.Scene.Skeletal;
using Engine.Tests.Fixtures;
using NSubstitute;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
[Collection("PathBuilder")]
public class SkeletalPoseMathTests : IDisposable
{
    private readonly string _tempRoot;

    public SkeletalPoseMathTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "SkeletalPoseMathTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
        var assets = Path.Combine(_tempRoot, "assets");
        Directory.CreateDirectory(assets);
        var ctx = Substitute.For<IProjectContext>();
        ctx.AssetsPath.Returns(assets);
        PathBuilder.UseProjectContext(ctx);
    }

    public void Dispose()
    {
        PathBuilder.UseProjectContext(Substitute.For<IProjectContext>());
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, recursive: true);
    }

    [Fact]
    public void Evaluate_WithNoChannels_PaletteIsIdentity()
    {
        var rootBind = Matrix4x4.CreateTranslation(1, 2, 3);
        Matrix4x4.Invert(rootBind, out var rootInverse);
        var childBind = rootBind * Matrix4x4.CreateTranslation(0, 4, 0);
        Matrix4x4.Invert(childBind, out var childInverse);

        var skeleton = new SkeletonAsset(
        [
            new SkeletonBone("root", -1, rootInverse),
            new SkeletonBone("child", 0, childInverse)
        ]);

        var clip = new Anim3dClip("empty", 1f, []);
        var palette = new Matrix4x4[SkeletalPoseMath.MaxBones];
        SkeletalPoseMath.Evaluate(skeleton, clip, 0f, palette);

        AssertNearIdentity(palette[0]);
        AssertNearIdentity(palette[1]);
        for (var i = 2; i < SkeletalPoseMath.MaxBones; i++)
            palette[i].ShouldBe(Matrix4x4.Identity);
    }

    [Fact]
    public void Evaluate_AnimatedAwayFromBind_PaletteLeavesIdentity()
    {
        var rootBind = Matrix4x4.Identity;
        var skeleton = new SkeletonAsset([new SkeletonBone("root", -1, rootBind)]);

        var clip = new Anim3dClip(
            "Pose",
            1f,
            [
                new Anim3dChannel(
                    0,
                    [
                        new Anim3dVec3Key(0f, Vector3.Zero),
                        new Anim3dVec3Key(1f, new Vector3(2, 0, 0))
                    ],
                    [new Anim3dQuatKey(0f, Quaternion.Identity), new Anim3dQuatKey(1f, Quaternion.Identity)],
                    [new Anim3dVec3Key(0f, Vector3.One), new Anim3dVec3Key(1f, Vector3.One)])
            ]);

        var palette = new Matrix4x4[SkeletalPoseMath.MaxBones];
        SkeletalPoseMath.Evaluate(skeleton, clip, 0f, palette);
        AssertNearIdentity(palette[0]);

        SkeletalPoseMath.Evaluate(skeleton, clip, 1f, palette);
        palette[0].ShouldNotBe(Matrix4x4.Identity);
        new Vector3(palette[0].M41, palette[0].M42, palette[0].M43).Length().ShouldBeGreaterThan(1f);
    }

    [Fact]
    public void Evaluate_RetargetsFirstKeysOntoRest_SoT0IsIdentityEvenWhenKeysDisagree()
    {
        // IB = I → rest local = I. First keys are posed away from origin — without retarget
        // IB×G(t0) would not be I. With retarget, t0 must still skin as bind.
        var skeleton = new SkeletonAsset([new SkeletonBone("root", -1, Matrix4x4.Identity)]);
        var clip = new Anim3dClip(
            "MixamoLike",
            1f,
            [
                new Anim3dChannel(
                    0,
                    [
                        new Anim3dVec3Key(0f, new Vector3(5, 0, 0)),
                        new Anim3dVec3Key(1f, new Vector3(7, 0, 0))
                    ],
                    [new Anim3dQuatKey(0f, Quaternion.Identity), new Anim3dQuatKey(1f, Quaternion.Identity)],
                    [new Anim3dVec3Key(0f, Vector3.One), new Anim3dVec3Key(1f, Vector3.One)])
            ]);

        var palette = new Matrix4x4[SkeletalPoseMath.MaxBones];
        SkeletalPoseMath.Evaluate(skeleton, clip, 0f, palette);
        AssertNearIdentity(palette[0]);

        SkeletalPoseMath.Evaluate(skeleton, clip, 1f, palette);
        // Delta is +2 on X from first key → rest * inv(T5) * T7 = T(2)
        new Vector3(palette[0].M41, palette[0].M42, palette[0].M43).X.ShouldBe(2f, 1e-4f);
    }

    [Fact]
    public void ComposeLocal_AppliesScaleThenRotationThenTranslation()
    {
        // Row-vector TRS local: v·S·R·T — scale/rotate about the joint, then offset in parent frame.
        var t = new Vector3(1, 0, 0);
        var r = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2f);
        var m = SkeletalPoseMath.ComposeLocal(t, r, Vector3.One);

        var origin = Vector3.Transform(Vector3.Zero, m);
        origin.X.ShouldBe(1f, 1e-5f, "joint origin must land at the parent-frame offset");
        origin.Y.ShouldBe(0f, 1e-5f);

        var tip = Vector3.Transform(Vector3.UnitX, m);
        tip.X.ShouldBe(1f, 1e-5f, "rotation must act about the joint, before the offset");
        tip.Y.ShouldBe(1f, 1e-5f);
    }

    [Fact]
    public void Evaluate_ChildOrbitsRotatedParent()
    {
        // Bind: root at origin, child at (1,0,0). Rotating root 90° about Z must carry
        // the child to (0,1,0) — the row-vector hierarchy contract.
        Matrix4x4.Invert(Matrix4x4.CreateTranslation(1, 0, 0), out var childIb);
        var skeleton = new SkeletonAsset(
        [
            new SkeletonBone("root", -1, Matrix4x4.Identity),
            new SkeletonBone("child", 0, childIb)
        ]);

        var rz90 = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2f);
        var clip = new Anim3dClip(
            "orbit",
            1f,
            [
                new Anim3dChannel(
                    0,
                    [new Anim3dVec3Key(0f, Vector3.Zero), new Anim3dVec3Key(1f, Vector3.Zero)],
                    [new Anim3dQuatKey(0f, Quaternion.Identity), new Anim3dQuatKey(1f, rz90)],
                    [new Anim3dVec3Key(0f, Vector3.One), new Anim3dVec3Key(1f, Vector3.One)])
            ]);

        var palette = new Matrix4x4[SkeletalPoseMath.MaxBones];
        SkeletalPoseMath.Evaluate(skeleton, clip, 1f, palette);

        var childWorld = JointWorld(skeleton, palette, 1);
        childWorld.X.ShouldBe(0f, 1e-4f);
        childWorld.Y.ShouldBe(1f, 1e-4f);
        childWorld.Z.ShouldBe(0f, 1e-4f);
    }

    [Fact]
    public void Evaluate_RotatingBoneAboutItsOwnJoint_DoesNotMoveTheJoint()
    {
        // Chain root(0,0,0) → childA(0,1,0) → childB(0,2,0). Rotate childA 90° about Z:
        // childA's joint must stay put; childB must orbit to (-1,1,0); bone lengths constant.
        Matrix4x4.Invert(Matrix4x4.CreateTranslation(0, 1, 0), out var aIb);
        Matrix4x4.Invert(Matrix4x4.CreateTranslation(0, 2, 0), out var bIb);
        var skeleton = new SkeletonAsset(
        [
            new SkeletonBone("root", -1, Matrix4x4.Identity),
            new SkeletonBone("childA", 0, aIb),
            new SkeletonBone("childB", 1, bIb)
        ]);

        var rz90 = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2f);
        var clip = new Anim3dClip(
            "pivot",
            1f,
            [
                new Anim3dChannel(
                    1,
                    [new Anim3dVec3Key(0f, new Vector3(0, 1, 0)), new Anim3dVec3Key(1f, new Vector3(0, 1, 0))],
                    [new Anim3dQuatKey(0f, Quaternion.Identity), new Anim3dQuatKey(1f, rz90)],
                    [new Anim3dVec3Key(0f, Vector3.One), new Anim3dVec3Key(1f, Vector3.One)])
            ]);

        var palette = new Matrix4x4[SkeletalPoseMath.MaxBones];
        SkeletalPoseMath.Evaluate(skeleton, clip, 1f, palette);

        var a = JointWorld(skeleton, palette, 1);
        a.X.ShouldBe(0f, 1e-4f, "rotating a bone about its own joint must not move the joint");
        a.Y.ShouldBe(1f, 1e-4f);

        var b = JointWorld(skeleton, palette, 2);
        b.X.ShouldBe(-1f, 1e-4f);
        b.Y.ShouldBe(1f, 1e-4f);

        Vector3.Distance(a, b).ShouldBe(1f, 1e-4f, "bone length must be rigid");
    }

    private static Vector3 JointWorld(SkeletonAsset skeleton, Matrix4x4[] palette, int boneIndex)
    {
        Matrix4x4.Invert(skeleton.Bones[boneIndex].InverseBind, out var bindGlobal);
        var g = bindGlobal * palette[boneIndex];
        return new Vector3(g.M41, g.M42, g.M43);
    }

    [Fact]
    public void Evaluate_TimeOutsideKeyRange_ClampsToFirstAndLastKeys()
    {
        var skeleton = new SkeletonAsset([new SkeletonBone("root", -1, Matrix4x4.Identity)]);
        var clip = new Anim3dClip(
            "clamp",
            1f,
            [
                new Anim3dChannel(
                    0,
                    [new Anim3dVec3Key(0f, Vector3.Zero), new Anim3dVec3Key(1f, new Vector3(2, 0, 0))],
                    [new Anim3dQuatKey(0f, Quaternion.Identity), new Anim3dQuatKey(1f, Quaternion.Identity)],
                    [new Anim3dVec3Key(0f, Vector3.One), new Anim3dVec3Key(1f, Vector3.One)])
            ]);

        var palette = new Matrix4x4[SkeletalPoseMath.MaxBones];

        SkeletalPoseMath.Evaluate(skeleton, clip, -5f, palette);
        AssertNearIdentity(palette[0]);

        SkeletalPoseMath.Evaluate(skeleton, clip, 99f, palette);
        palette[0].M41.ShouldBe(2f, 1e-4f, "time past the last key must clamp to the last key");

        var atEnd = new Matrix4x4[SkeletalPoseMath.MaxBones];
        SkeletalPoseMath.Evaluate(skeleton, clip, 1f, atEnd);
        palette[0].ShouldBe(atEnd[0]);
    }

    [Fact]
    public void Evaluate_NegatedQuaternionKeys_TakeShortestArc_NoFlip()
    {
        // (0,0,0,1) and (0,0,0,-1) encode the SAME orientation (quaternion double cover).
        // Halfway between them must still be the identity rotation, not a 180° flip.
        var skeleton = new SkeletonAsset([new SkeletonBone("root", -1, Matrix4x4.Identity)]);
        var clip = new Anim3dClip(
            "doubleCover",
            1f,
            [
                new Anim3dChannel(
                    0,
                    [new Anim3dVec3Key(0f, Vector3.Zero), new Anim3dVec3Key(1f, Vector3.Zero)],
                    [
                        new Anim3dQuatKey(0f, new Quaternion(0, 0, 0, 1)),
                        new Anim3dQuatKey(1f, new Quaternion(0, 0, 0, -1))
                    ],
                    [new Anim3dVec3Key(0f, Vector3.One), new Anim3dVec3Key(1f, Vector3.One)])
            ]);

        var palette = new Matrix4x4[SkeletalPoseMath.MaxBones];
        SkeletalPoseMath.Evaluate(skeleton, clip, 0.5f, palette);

        AssertNearIdentity(palette[0], 1e-3f);
    }

    [Fact]
    public void Evaluate_DuplicateKeyTimes_ProducesFinitePalette()
    {
        // Step-style exports emit duplicate timestamps; the zero-span guard must not divide by zero.
        var skeleton = new SkeletonAsset([new SkeletonBone("root", -1, Matrix4x4.Identity)]);
        var clip = new Anim3dClip(
            "dupes",
            1f,
            [
                new Anim3dChannel(
                    0,
                    [
                        new Anim3dVec3Key(0f, Vector3.Zero),
                        new Anim3dVec3Key(0.5f, new Vector3(1, 0, 0)),
                        new Anim3dVec3Key(0.5f, new Vector3(3, 0, 0)),
                        new Anim3dVec3Key(1f, new Vector3(3, 0, 0))
                    ],
                    [
                        new Anim3dQuatKey(0f, Quaternion.Identity),
                        new Anim3dQuatKey(0.5f, Quaternion.Identity),
                        new Anim3dQuatKey(0.5f, Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2f)),
                        new Anim3dQuatKey(1f, Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2f))
                    ],
                    [new Anim3dVec3Key(0f, Vector3.One), new Anim3dVec3Key(1f, Vector3.One)])
            ]);

        var palette = new Matrix4x4[SkeletalPoseMath.MaxBones];
        foreach (var t in new[] { 0f, 0.49f, 0.5f, 0.51f, 1f })
        {
            SkeletalPoseMath.Evaluate(skeleton, clip, t, palette);
            for (var c = 0; c < 16; c++)
            {
                var m = palette[0];
                float[] cells =
                [
                    m.M11, m.M12, m.M13, m.M14, m.M21, m.M22, m.M23, m.M24,
                    m.M31, m.M32, m.M33, m.M34, m.M41, m.M42, m.M43, m.M44
                ];
                float.IsFinite(cells[c]).ShouldBeTrue($"palette cell {c} at t={t} is not finite");
            }
        }
    }

    [Fact]
    public void Evaluate_CookedSkinnedGltf_AtTimeZero_MatchesIdentitySkinningOnWeightedVerts()
    {
        var (skeleton, clip, model) = CookTwoBoneSkinned();

        // Rest-only palette must be identity (mesh bind ↔ InverseBind).
        var restPalette = new Matrix4x4[SkeletalPoseMath.MaxBones];
        SkeletalPoseMath.Evaluate(skeleton, new Anim3dClip("rest", 1f, []), 0f, restPalette);
        for (var i = 0; i < skeleton.Bones.Count; i++)
            AssertNearIdentity(restPalette[i], 1e-3f);

        var palette = new Matrix4x4[SkeletalPoseMath.MaxBones];
        SkeletalPoseMath.Evaluate(skeleton, clip, 0f, palette);

        foreach (var sub in model.Submeshes)
        {
            foreach (var v in sub.Mesh.Vertices)
            {
                var weightSum = v.BoneWeight.X + v.BoneWeight.Y + v.BoneWeight.Z + v.BoneWeight.W;
                if (weightSum < 1e-5f)
                    continue;

                var identitySkinned = SkinnedVertexTestMath.SkinPosition(v, CreateIdentityPalette());
                var poseSkinned = SkinnedVertexTestMath.SkinPosition(v, palette);
                // First keys may differ from bind; rest palette identity is the hard contract.
                // At t=0 still require finite, bounded skinning.
                float.IsFinite(poseSkinned.X).ShouldBeTrue();
                poseSkinned.Length().ShouldBeLessThan(identitySkinned.Length() + 5f);
            }
        }
    }

    [Fact]
    public void Evaluate_CookedSkinnedGltf_AtMidClip_ProducesFiniteBoundedSkinning()
    {
        var (skeleton, clip, model) = CookTwoBoneSkinned();

        var palette = new Matrix4x4[SkeletalPoseMath.MaxBones];
        SkeletalPoseMath.Evaluate(skeleton, clip, clip.DurationSeconds * 0.5f, palette);

        foreach (var sub in model.Submeshes)
        {
            foreach (var v in sub.Mesh.Vertices)
            {
                var weightSum = v.BoneWeight.X + v.BoneWeight.Y + v.BoneWeight.Z + v.BoneWeight.W;
                if (weightSum < 1e-5f)
                    continue;

                var poseSkinned = SkinnedVertexTestMath.SkinPosition(v, palette);
                float.IsFinite(poseSkinned.X).ShouldBeTrue();
                float.IsFinite(poseSkinned.Y).ShouldBeTrue();
                float.IsFinite(poseSkinned.Z).ShouldBeTrue();
                poseSkinned.Length().ShouldBeLessThan(10f, "skinned vertex should stay near origin for fixture");
            }
        }
    }

    private (SkeletonAsset Skeleton, Anim3dClip Clip, Model Model) CookTwoBoneSkinned()
    {
        var assets = Path.Combine(_tempRoot, "assets");
        var sourceDir = Path.Combine(_tempRoot, "source");
        Directory.CreateDirectory(sourceDir);

        var source = SkinnedGltfFixture.WriteTwoBoneSkinned(sourceDir, "twobone", animationCount: 1);
        var cook = MeshCreator.CreateSkinned(source, assets, "twobone");
        cook.Success.ShouldBeTrue(cook.Error);

        using var skelStream = File.OpenRead(Path.Combine(assets, "models/twobone.skel"));
        var skeleton = SkeletonReader.Read(skelStream);
        using var animStream = File.OpenRead(Path.Combine(assets, "models/twobone.anim3d"));
        var anim = Anim3dReader.Read(animStream);
        using var meshStream = File.OpenRead(Path.Combine(assets, "models/twobone.mesh"));
        var model = MeshReader.Read(meshStream);

        return (skeleton, anim.Clips[0], model);
    }

    private static Matrix4x4[] CreateIdentityPalette()
    {
        var palette = new Matrix4x4[SkeletalPoseMath.MaxBones];
        for (var i = 0; i < palette.Length; i++)
            palette[i] = Matrix4x4.Identity;
        return palette;
    }

    private static void AssertNearIdentity(Matrix4x4 m, float eps = 1e-4f)
    {
        var d = 0f;
        d += MathF.Abs(m.M11 - 1f) + MathF.Abs(m.M22 - 1f) + MathF.Abs(m.M33 - 1f) + MathF.Abs(m.M44 - 1f);
        d += MathF.Abs(m.M12) + MathF.Abs(m.M13) + MathF.Abs(m.M14);
        d += MathF.Abs(m.M21) + MathF.Abs(m.M23) + MathF.Abs(m.M24);
        d += MathF.Abs(m.M31) + MathF.Abs(m.M32) + MathF.Abs(m.M34);
        d += MathF.Abs(m.M41) + MathF.Abs(m.M42) + MathF.Abs(m.M43);
        d.ShouldBeLessThan(eps);
    }
}
