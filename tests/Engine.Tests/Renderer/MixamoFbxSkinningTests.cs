using System.Numerics;
using Engine.Core;
using Engine.Renderer;
using Engine.Scene;
using NSubstitute;
using Shouldly;
using Xunit.Abstractions;

namespace Engine.Tests.Renderer;

/// <summary>Regression for Mixamo FBX skinning when source file is present locally.</summary>
[Trait("Category", "Unit")]
[Collection("PathBuilder")]
public class MixamoFbxSkinningTests : IDisposable
{
    private const string FbxPath = "/Users/mateuszkulesza/Downloads/Illegal Elbow Punch.fbx";
    private readonly string _tempRoot;
    private readonly ITestOutputHelper _output;

    public MixamoFbxSkinningTests(ITestOutputHelper output)
    {
        _output = output;
        _tempRoot = Path.Combine(Path.GetTempPath(), "MixamoFbxSkin-" + Guid.NewGuid().ToString("N"));
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
    public void FreshCook_Fbx_RestPaletteIdentity_AndMidClipBounded()
    {
        if (!File.Exists(FbxPath))
            return;

        var assets = Path.Combine(_tempRoot, "assets");
        var cook = MeshCreator.CreateSkinned(FbxPath, assets, "elbow");
        cook.Success.ShouldBeTrue(cook.Error);

        using var skelStream = File.OpenRead(Path.Combine(assets, "models/elbow.skel"));
        var skeleton = SkeletonReader.Read(skelStream);
        using var animStream = File.OpenRead(Path.Combine(assets, "models/elbow.anim3d"));
        var anim = Anim3dReader.Read(animStream);
        using var meshStream = File.OpenRead(Path.Combine(assets, "models/elbow.mesh"));
        var model = MeshReader.Read(meshStream);

        var clip = anim.Clips[0];
        var bindExtent = MeshExtent(model);
        var maxKeyT = 0f;
        foreach (var ch in clip.Channels)
        foreach (var k in ch.TranslationKeys)
            maxKeyT = MathF.Max(maxKeyT, k.Value.Length());
        _output.WriteLine($"bones={skeleton.Bones.Count} channels={clip.Channels.Count} bindExtent={bindExtent:F2} maxKeyT={maxKeyT:F2}");
        foreach (var b in skeleton.Bones.Take(3))
            _output.WriteLine($"  bone {b.Name} parent={b.ParentIndex} ibT={new Vector3(b.InverseBind.M41, b.InverseBind.M42, b.InverseBind.M43).Length():F3}");

        var rest = new Matrix4x4[SkeletalPoseMath.MaxBones];
        SkeletalPoseMath.Evaluate(skeleton, new Anim3dClip("rest", 1f, []), 0f, rest);
        for (var i = 0; i < skeleton.Bones.Count; i++)
            MatrixDev(rest[i]).ShouldBeLessThan(1e-2f, $"rest bone {i} ({skeleton.Bones[i].Name})");

        // Rest skinning must match raw bind-pose verts.
        foreach (var sub in model.Submeshes)
        foreach (var v in sub.Mesh.Vertices)
        {
            var w = v.BoneWeight.X + v.BoneWeight.Y + v.BoneWeight.Z + v.BoneWeight.W;
            if (w < 1e-5f) continue;
            var p = SkinnedVertexTestMath.SkinPosition(v, rest);
            Vector3.Distance(v.Position, p).ShouldBeLessThan(0.05f);
            break;
        }

        // Channel retarget: first-key time must skin as bind (IB×G ≈ I).
        var t0 = 0f;
        foreach (var ch in clip.Channels)
            t0 = MathF.Max(t0, SkeletalPoseMath.ChannelBindTime(ch));
        var atT0 = new Matrix4x4[SkeletalPoseMath.MaxBones];
        SkeletalPoseMath.Evaluate(skeleton, clip, t0, atT0);
        var t0Dev = 0f;
        for (var i = 0; i < skeleton.Bones.Count; i++)
            t0Dev = MathF.Max(t0Dev, MatrixDev(atT0[i]));
        _output.WriteLine($"t0={t0:F4} t0PaletteMaxDev={t0Dev:F5}");
        t0Dev.ShouldBeLessThan(1e-2f, "Evaluate at first keys must match bind pose");
        foreach (var sub in model.Submeshes)
        foreach (var v in sub.Mesh.Vertices)
        {
            var w = v.BoneWeight.X + v.BoneWeight.Y + v.BoneWeight.Z + v.BoneWeight.W;
            if (w < 1e-5f) continue;
            var p = SkinnedVertexTestMath.SkinPosition(v, atT0);
            Vector3.Distance(v.Position, p).ShouldBeLessThan(0.05f, "t0 cpuSkin must match bind vert");
            break;
        }

        // Rotation tracks must survive the native interop: sorted times spanning the clip, unit quats.
        // (Guards the aiQuatKey ABI-stride corruption that froze live palettes.)
        foreach (var ch in clip.Channels)
        {
            if (ch.RotationKeys.Count < 2)
                continue;
            for (var i = 1; i < ch.RotationKeys.Count; i++)
                (ch.RotationKeys[i].Time >= ch.RotationKeys[i - 1].Time).ShouldBeTrue(
                    $"bone {skeleton.Bones[(int)ch.BoneIndex].Name} rotation key {i} time out of order");
            ch.RotationKeys[^1].Time.ShouldBeGreaterThan(
                clip.DurationSeconds * 0.9f,
                $"bone {skeleton.Bones[(int)ch.BoneIndex].Name} rotation track must span the clip");
            foreach (var k in ch.RotationKeys)
                MathF.Abs(k.Value.Length() - 1f).ShouldBeLessThan(
                    0.02f, $"bone {skeleton.Bones[(int)ch.BoneIndex].Name} rotation key at t={k.Time} not unit length");
        }

        // Continuity one frame after start: pose must still be ≈ bind, not an instant explode.
        var eps = new Matrix4x4[SkeletalPoseMath.MaxBones];
        SkeletalPoseMath.Evaluate(skeleton, clip, 0.006f, eps);
        var epsDev = 0f;
        for (var i = 0; i < skeleton.Bones.Count; i++)
            epsDev = MathF.Max(epsDev, MatrixDev(eps[i]));
        epsDev.ShouldBeLessThan(1f, "palette 6ms after start must remain near bind pose");

        var mid = new Matrix4x4[SkeletalPoseMath.MaxBones];
        SkeletalPoseMath.Evaluate(skeleton, clip, 0.25f * clip.DurationSeconds, mid);
        var extent = SkinnedExtent(model, mid);
        var maxDisp = 0f;
        foreach (var sub in model.Submeshes)
        foreach (var v in sub.Mesh.Vertices)
        {
            var w = v.BoneWeight.X + v.BoneWeight.Y + v.BoneWeight.Z + v.BoneWeight.W;
            if (w < 1e-5f) continue;
            maxDisp = MathF.Max(maxDisp, Vector3.Distance(v.Position, SkinnedVertexTestMath.SkinPosition(v, mid)));
        }
        _output.WriteLine($"mid extent={extent:F2} maxT={MaxPaletteTranslation(mid, skeleton.Bones.Count):F2} maxDisp={maxDisp:F2}");
        MaxPaletteTranslation(mid, skeleton.Bones.Count).ShouldBeLessThan(50f);
        // Elbow punch AABB can grow ~5× bind; guard against explode (~10×+) not artistic reach.
        extent.ShouldBeLessThan(bindExtent * 6f);
        maxDisp.ShouldBeLessThan(bindExtent * 3f);

        // Skeleton must stay rigid across the whole clip: parent-child joint distances
        // may not stretch (catches wrong-order hierarchy composition).
        var bindGlobals = new Matrix4x4[skeleton.Bones.Count];
        for (var i = 0; i < skeleton.Bones.Count; i++)
            Matrix4x4.Invert(skeleton.Bones[i].InverseBind, out bindGlobals[i]);
        var bindPos = new Vector3[skeleton.Bones.Count];
        for (var i = 0; i < skeleton.Bones.Count; i++)
            bindPos[i] = new Vector3(bindGlobals[i].M41, bindGlobals[i].M42, bindGlobals[i].M43);
        var sweep = new Matrix4x4[SkeletalPoseMath.MaxBones];
        for (var f = 0; f <= 32; f++)
        {
            SkeletalPoseMath.Evaluate(skeleton, clip, clip.DurationSeconds * f / 32f, sweep);
            for (var i = 0; i < skeleton.Bones.Count; i++)
            {
                var p = skeleton.Bones[i].ParentIndex;
                if (p < 0)
                    continue;
                var bindLen = Vector3.Distance(bindPos[i], bindPos[p]);
                if (bindLen < 1e-3f)
                    continue;
                var gi = bindGlobals[i] * sweep[i];
                var gp = bindGlobals[p] * sweep[p];
                var len = Vector3.Distance(
                    new Vector3(gi.M41, gi.M42, gi.M43), new Vector3(gp.M41, gp.M42, gp.M43));
                MathF.Abs(len / bindLen - 1f).ShouldBeLessThan(
                    0.05f, $"bone '{skeleton.Bones[i].Name}' stretched at frame {f}");
            }
        }

        // Weighted bones should change across the clip.
        var mid2 = new Matrix4x4[SkeletalPoseMath.MaxBones];
        SkeletalPoseMath.Evaluate(skeleton, clip, 0.75f * clip.DurationSeconds, mid2);
        var changed = false;
        for (var i = 0; i < skeleton.Bones.Count; i++)
        {
            var d = MathF.Abs(mid[i].M41 - mid2[i].M41) + MathF.Abs(mid[i].M42 - mid2[i].M42) + MathF.Abs(mid[i].M43 - mid2[i].M43);
            if (d > 0.01f) { changed = true; break; }
        }
        changed.ShouldBeTrue("palette should change across clip");


        cook.Parts.ShouldNotBeEmpty();
        cook.Parts[0].Rotation.Y.ShouldBe(0f, 0.01f);
        MathF.Abs(cook.Parts[0].Rotation.X).ShouldBeLessThan(0.01f);
        cook.Parts[0].Translation.Length().ShouldBeLessThan(0.01f);

        foreach (var sub in model.Submeshes)
        foreach (var v in sub.Mesh.Vertices)
        {
            var w = v.BoneWeight.X + v.BoneWeight.Y + v.BoneWeight.Z + v.BoneWeight.W;
            if (w < 1e-5f) continue;
            SkinnedVertexTestMath.AssertAffineSkinWeights(v, mid);
            break;
        }
    }

    private static float MatrixDev(Matrix4x4 m)
    {
        var d = 0f;
        d += MathF.Abs(m.M11 - 1f) + MathF.Abs(m.M22 - 1f) + MathF.Abs(m.M33 - 1f) + MathF.Abs(m.M44 - 1f);
        d += MathF.Abs(m.M12) + MathF.Abs(m.M13) + MathF.Abs(m.M14);
        d += MathF.Abs(m.M21) + MathF.Abs(m.M23) + MathF.Abs(m.M24);
        d += MathF.Abs(m.M31) + MathF.Abs(m.M32) + MathF.Abs(m.M34);
        d += MathF.Abs(m.M41) + MathF.Abs(m.M42) + MathF.Abs(m.M43);
        return d;
    }

    private static float MeshExtent(Model model)
    {
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        foreach (var sub in model.Submeshes)
        foreach (var v in sub.Mesh.Vertices)
        {
            min = Vector3.Min(min, v.Position);
            max = Vector3.Max(max, v.Position);
        }
        return (max - min).Length();
    }

    private static float SkinnedExtent(Model model, Matrix4x4[] palette)
    {
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        foreach (var sub in model.Submeshes)
        foreach (var v in sub.Mesh.Vertices)
        {
            var w = v.BoneWeight.X + v.BoneWeight.Y + v.BoneWeight.Z + v.BoneWeight.W;
            if (w < 1e-5f) continue;
            var p = SkinnedVertexTestMath.SkinPosition(v, palette);
            if (!float.IsFinite(p.X)) return float.PositiveInfinity;
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
        }
        return (max - min).Length();
    }

    private static float MaxPaletteTranslation(Matrix4x4[] palette, int boneCount)
    {
        var max = 0f;
        for (var i = 0; i < boneCount; i++)
        {
            var t = new Vector3(palette[i].M41, palette[i].M42, palette[i].M43);
            max = MathF.Max(max, t.Length());
        }
        return max;
    }
}
