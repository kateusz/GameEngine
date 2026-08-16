using System.Numerics;
using Editor.Features.Import;
using Engine.Renderer.Models;
using Shouldly;
using Silk.NET.Assimp;
using SkeletonBone = Engine.Renderer.Models.SkeletonBone;

namespace Editor.Tests.Import;

[Trait("Category", "Unit")]
public class MixamoFbxImportTests
{
    private static readonly string Fbx = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "games", "Arena3D", "assets", "models", "ja-Walking.fbx"));

    [Fact]
    public void ImportSkinned_MixamoWalking_RootKeysMatchMeshSpace()
    {
        System.IO.File.Exists(Fbx).ShouldBeTrue($"missing Mixamo FBX at {Fbx}");

        using var assimp = Assimp.GetApi();
        var imported = new AssimpModelImporter(assimp).ImportSkinned(Fbx);
        imported.ShouldNotBeNull();

        imported.Bones.ShouldNotContain(b => b.Name.Contains("$AssimpFbx$", StringComparison.Ordinal));

        var rest = RestLocals(imported.Bones);
        var hips = imported.Bones.Select((b, i) => (b, i))
            .First(x => x.b.Name.Contains("Hips", StringComparison.OrdinalIgnoreCase));
        var channel = imported.Clips[0].Channels.First(c => c.BoneIndex == hips.i);

        Matrix4x4.Decompose(rest[hips.i], out _, out var restR, out var restT);
        var keyT = channel.Positions[0].Value;
        var keyR = Quaternion.Normalize(channel.Rotations[0].Value);

        (keyT - restT).Length().ShouldBeLessThan(15f);
        AngleDeg(restR, keyR).ShouldBeLessThan(25f);
    }

    private static float AngleDeg(Quaternion a, Quaternion b)
    {
        var dot = MathF.Min(1f, MathF.Abs(Quaternion.Dot(Quaternion.Normalize(a), Quaternion.Normalize(b))));
        return 2f * MathF.Acos(dot) * (180f / MathF.PI);
    }

    private static Matrix4x4[] RestLocals(IReadOnlyList<SkeletonBone> bones)
    {
        var bindGlobals = new Matrix4x4[bones.Count];
        var rest = new Matrix4x4[bones.Count];
        for (var i = 0; i < bones.Count; i++)
        {
            if (!Matrix4x4.Invert(bones[i].InverseBind, out bindGlobals[i]))
                bindGlobals[i] = Matrix4x4.Identity;
        }

        for (var i = 0; i < bones.Count; i++)
        {
            var parent = bones[i].ParentIndex;
            if (parent < 0)
            {
                rest[i] = bindGlobals[i];
                continue;
            }

            if (!Matrix4x4.Invert(bindGlobals[parent], out var invParent))
                invParent = Matrix4x4.Identity;
            rest[i] = bindGlobals[i] * invParent;
        }

        return rest;
    }
}
