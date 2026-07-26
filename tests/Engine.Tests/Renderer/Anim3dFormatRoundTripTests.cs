using System.Numerics;
using Engine.Renderer;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class Anim3dFormatRoundTripTests
{
    [Fact]
    public void RoundTrip_MultiClip_PreservesSparseTrsKeysAndDurationSeconds()
    {
        var clipA = new Anim3dClip(
            "Idle",
            1.5f,
            [
                new Anim3dChannel(
                    0,
                    [new Anim3dVec3Key(0f, Vector3.Zero), new Anim3dVec3Key(1.5f, new Vector3(1, 0, 0))],
                    [new Anim3dQuatKey(0f, Quaternion.Identity)],
                    [])
            ]);

        var clipB = new Anim3dClip(
            "Walk",
            2f,
            [
                new Anim3dChannel(
                    1,
                    [],
                    [
                        new Anim3dQuatKey(0f, Quaternion.Identity),
                        new Anim3dQuatKey(1f, Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2f))
                    ],
                    [new Anim3dVec3Key(0f, Vector3.One), new Anim3dVec3Key(2f, new Vector3(1, 1, 1))])
            ]);

        var asset = new Anim3dAsset([clipA, clipB]);

        using var stream = new MemoryStream();
        Anim3dWriter.Write(stream, asset);
        stream.Position = 0;
        var loaded = Anim3dReader.Read(stream);

        loaded.Clips.Count.ShouldBe(2);

        loaded.Clips[0].Name.ShouldBe("Idle");
        loaded.Clips[0].DurationSeconds.ShouldBe(1.5f);
        loaded.Clips[0].Channels.Count.ShouldBe(1);
        loaded.Clips[0].Channels[0].BoneIndex.ShouldBe(0u);
        loaded.Clips[0].Channels[0].TranslationKeys.Count.ShouldBe(2);
        loaded.Clips[0].Channels[0].TranslationKeys[1].Value.ShouldBe(new Vector3(1, 0, 0));
        loaded.Clips[0].Channels[0].RotationKeys.Count.ShouldBe(1);
        loaded.Clips[0].Channels[0].ScaleKeys.Count.ShouldBe(0);

        loaded.Clips[1].Name.ShouldBe("Walk");
        loaded.Clips[1].DurationSeconds.ShouldBe(2f);
        loaded.Clips[1].Channels[0].BoneIndex.ShouldBe(1u);
        loaded.Clips[1].Channels[0].TranslationKeys.Count.ShouldBe(0);
        loaded.Clips[1].Channels[0].RotationKeys.Count.ShouldBe(2);
        loaded.Clips[1].Channels[0].ScaleKeys.Count.ShouldBe(2);
        loaded.Clips[1].Channels[0].ScaleKeys[0].Value.ShouldBe(Vector3.One);
    }

    [Fact]
    public void Read_RejectsBoneIndexAtOrAboveMaxBones()
    {
        var clip = new Anim3dClip(
            "Bad",
            1f,
            [new Anim3dChannel(SkeletonReader.MaxBones, [], [], [])]);

        using var stream = new MemoryStream();
        Anim3dWriter.Write(stream, new Anim3dAsset([clip]));
        stream.Position = 0;

        Should.Throw<InvalidDataException>(() => Anim3dReader.Read(stream))
            .Message.ShouldContain("boneIndex");
    }
}
