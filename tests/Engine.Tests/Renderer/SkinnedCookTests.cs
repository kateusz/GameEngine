using System.Numerics;
using System.Text;
using Engine.Core;
using Engine.Renderer;
using Engine.Renderer.Serialization;
using Engine.Renderer.Skeletal.Serialization;
using Engine.Tests.Fixtures;
using NSubstitute;
using Shouldly;
using Silk.NET.Assimp;
using File = System.IO.File;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
[Collection("PathBuilder")]
public class SkinnedCookTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _assetsRoot;
    private readonly string _sourceDir;

    public SkinnedCookTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "GameEngine-SkinnedCookTests", Guid.NewGuid().ToString("N"));
        _assetsRoot = Path.Combine(_tempRoot, "assets");
        _sourceDir = Path.Combine(_tempRoot, "source");
        Directory.CreateDirectory(_assetsRoot);
        Directory.CreateDirectory(_sourceDir);

        var context = Substitute.For<IProjectContext>();
        context.AssetsPath.Returns(_assetsRoot);
        PathBuilder.UseProjectContext(context);
    }

    public void Dispose()
    {
        PathBuilder.UseProjectContext(Substitute.For<IProjectContext>());
        try
        {
            if (Directory.Exists(_tempRoot))
                Directory.Delete(_tempRoot, recursive: true);
        }
        catch
        {
        }
    }

    [Fact]
    public void ImportSkinned_PostProcessFlags_OmitPreTransform_IncludeLimitBoneWeights()
    {
        var flags = AssimpModelImporter.SkinnedPostProcessFlags;

        flags.HasFlag(PostProcessSteps.PreTransformVertices).ShouldBeFalse();
        flags.HasFlag(PostProcessSteps.JoinIdenticalVertices).ShouldBeTrue();
        flags.HasFlag(PostProcessSteps.LimitBoneWeights).ShouldBeTrue();
        flags.HasFlag(PostProcessSteps.Triangulate).ShouldBeTrue();
        flags.HasFlag(PostProcessSteps.GenerateNormals).ShouldBeTrue();
        flags.HasFlag(PostProcessSteps.CalculateTangentSpace).ShouldBeTrue();
    }

    [Fact]
    public void CreateSkinned_ExtractsBoneIndicesAndWeightsOntoVertices()
    {
        var source = SkinnedGltfFixture.WriteTwoBoneSkinned(_sourceDir, "twobone", animationCount: 1);
        var stem = "twobone";

        var result = MeshCreator.CreateSkinned(source, _assetsRoot, stem);

        result.Success.ShouldBeTrue(result.Error);
        result.MeshRelativePath.ShouldBe($"models/{stem}.mesh");
        result.SkeletonRelativePath.ShouldBe($"models/{stem}.skel");
        result.Anim3dRelativePath.ShouldBe($"models/{stem}.anim3d");

        File.Exists(Path.Combine(_assetsRoot, "models", $"{stem}.mesh")).ShouldBeTrue();
        File.Exists(Path.Combine(_assetsRoot, "models", $"{stem}.skel")).ShouldBeTrue();
        File.Exists(Path.Combine(_assetsRoot, "models", $"{stem}.anim3d")).ShouldBeTrue();

        using var meshStream = File.OpenRead(Path.Combine(_assetsRoot, "models", $"{stem}.mesh"));
        var model = MeshReader.Read(meshStream);
        model.Submeshes.Count.ShouldBeGreaterThan(0);

        var anyWeighted = false;
        foreach (var sub in model.Submeshes)
        {
            foreach (var v in sub.Mesh.Vertices)
            {
                var weightSum = v.BoneWeight.X + v.BoneWeight.Y + v.BoneWeight.Z + v.BoneWeight.W;
                var nonZero = 0;
                if (v.BoneWeight.X > 0) nonZero++;
                if (v.BoneWeight.Y > 0) nonZero++;
                if (v.BoneWeight.Z > 0) nonZero++;
                if (v.BoneWeight.W > 0) nonZero++;
                nonZero.ShouldBeLessThanOrEqualTo(4);

                if (weightSum > 0.01f)
                {
                    anyWeighted = true;
                    weightSum.ShouldBe(1f, 0.05f);
                    v.BoneIndex.X.ShouldBeGreaterThanOrEqualTo(0);
                }
            }
        }

        anyWeighted.ShouldBeTrue("expected at least one vertex with bone weights");

        using var skelStream = File.OpenRead(Path.Combine(_assetsRoot, "models", $"{stem}.skel"));
        var skel = SkeletonReader.Read(skelStream);
        skel.Bones.Count.ShouldBeGreaterThanOrEqualTo(2);
        skel.Bones.Count.ShouldBeLessThanOrEqualTo(100);
    }

    [Fact]
    public void CreateSkinned_MoreThan100Bones_FailsWithClearError()
    {
        var source = SkinnedGltfFixture.WriteManyBones(_sourceDir, "toomany", boneCount: 101);
        var stem = "toomany";

        var result = MeshCreator.CreateSkinned(source, _assetsRoot, stem);

        result.Success.ShouldBeFalse();
        result.Error.ShouldNotBeNullOrWhiteSpace();
        result.Error!.ShouldContain("100");
        result.MeshRelativePath.ShouldBeNull();
        File.Exists(Path.Combine(_assetsRoot, "models", $"{stem}.mesh")).ShouldBeFalse();
        File.Exists(Path.Combine(_assetsRoot, "models", $"{stem}.skel")).ShouldBeFalse();
    }

    [Fact]
    public void CreateSkinned_MultiClipSource_WritesAnim3dWithClipCountAtLeast2()
    {
        var source = SkinnedGltfFixture.WriteTwoBoneSkinned(_sourceDir, "multiclip", animationCount: 2);
        var stem = "multiclip";

        var result = MeshCreator.CreateSkinned(source, _assetsRoot, stem);

        result.Success.ShouldBeTrue(result.Error);

        using var animStream = File.OpenRead(Path.Combine(_assetsRoot, "models", $"{stem}.anim3d"));
        var anim = Anim3dReader.Read(animStream);
        anim.Clips.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void CreateSplit_StaticSource_WritesZeroBoneAttrs()
    {
        var sourcePath = WriteObjTriangle(_sourceDir, "staticv2");
        var stem = Path.GetFileNameWithoutExtension(sourcePath);

        var result = MeshCreator.CreateSplit(sourcePath, _assetsRoot, stem);

        result.Success.ShouldBeTrue(result.Error);

        var meshAbsolute = Path.Combine(_assetsRoot, "models", $"{stem}.mesh");
        using var stream = File.OpenRead(meshAbsolute);
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        Encoding.ASCII.GetString(reader.ReadBytes(4)).ShouldBe("KULA");
        reader.ReadUInt32().ShouldBe(MeshReader.FormatVersion);
        reader.ReadUInt32().ShouldBe(1u);

        using var meshStream = File.OpenRead(meshAbsolute);
        var model = MeshReader.Read(meshStream);
        foreach (var sub in model.Submeshes)
        {
            foreach (var v in sub.Mesh.Vertices)
            {
                v.BoneIndex.ShouldBe(default(Vector4));
                v.BoneWeight.ShouldBe(default(System.Numerics.Vector4));
            }
        }
    }

    private static string WriteObjTriangle(string dir, string stem)
    {
        var path = Path.Combine(dir, $"{stem}.obj");
        File.WriteAllText(path, """
            v 0 0 0
            v 1 0 0
            v 0 1 0
            vn 0 0 1
            f 1//1 2//1 3//1
            """);
        return path;
    }
}
