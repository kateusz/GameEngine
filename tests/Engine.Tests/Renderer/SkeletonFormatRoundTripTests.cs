using System.Numerics;
using Engine.Renderer.Skeletal;
using Engine.Renderer.Skeletal.Serialization;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class SkeletonFormatRoundTripTests
{
    [Fact]
    public void RoundTrip_PreservesBoneNamesParentIndexAndInverseBind()
    {
        var rootBind = Matrix4x4.CreateTranslation(1, 2, 3);
        var childBind = Matrix4x4.CreateScale(2f) * Matrix4x4.CreateTranslation(0, 1, 0);

        var asset = new SkeletonAsset([
            new SkeletonBone("Root", -1, rootBind),
            new SkeletonBone("Child", 0, childBind)
        ]);

        using var stream = new MemoryStream();
        SkeletonWriter.Write(stream, asset);
        stream.Position = 0;
        var loaded = SkeletonReader.Read(stream);

        loaded.Bones.Count.ShouldBe(2);
        loaded.Bones[0].Name.ShouldBe("Root");
        loaded.Bones[0].ParentIndex.ShouldBe(-1);
        loaded.Bones[0].InverseBind.ShouldBe(rootBind);
        loaded.Bones[1].Name.ShouldBe("Child");
        loaded.Bones[1].ParentIndex.ShouldBe(0);
        loaded.Bones[1].InverseBind.ShouldBe(childBind);
    }

    [Fact]
    public void Read_RejectsInvalidParentIndexAndCycles()
    {
        var outOfRange = new SkeletonAsset([new SkeletonBone("root", 5, Matrix4x4.Identity)]);
        using (var stream = new MemoryStream())
        {
            SkeletonWriter.Write(stream, outOfRange);
            stream.Position = 0;
            Should.Throw<InvalidDataException>(() => SkeletonReader.Read(stream))
                .Message.ShouldContain("parentIndex");
        }

        var selfParent = new SkeletonAsset([new SkeletonBone("root", 0, Matrix4x4.Identity)]);
        using (var stream = new MemoryStream())
        {
            SkeletonWriter.Write(stream, selfParent);
            stream.Position = 0;
            Should.Throw<InvalidDataException>(() => SkeletonReader.Read(stream))
                .Message.ShouldContain("own parent");
        }

        var cycle = new SkeletonAsset(
        [
            new SkeletonBone("a", 1, Matrix4x4.Identity),
            new SkeletonBone("b", 0, Matrix4x4.Identity)
        ]);
        using (var stream = new MemoryStream())
        {
            SkeletonWriter.Write(stream, cycle);
            stream.Position = 0;
            Should.Throw<InvalidDataException>(() => SkeletonReader.Read(stream))
                .Message.ShouldContain("cycle");
        }
    }
}
