using System.Numerics;
using Engine.Core;
using Engine.Renderer;
using Engine.Scene;
using Engine.Tests.Fixtures;
using NSubstitute;
using Shouldly;
using File = System.IO.File;

namespace Engine.Tests.Renderer;

/// <summary>
/// Rotation keys must survive the Assimp interop cook byte-for-byte sane.
/// Guards against managed/native aiQuatKey layout (ABI) mismatches: only key 0 survives a
/// stride mismatch, so the fixture uses 3 keys.
/// </summary>
[Trait("Category", "Unit")]
[Collection("PathBuilder")]
public class SkinnedAnimRotationKeysTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _assetsRoot;
    private readonly string _sourceDir;

    public SkinnedAnimRotationKeysTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "GameEngine-SkinnedRotKeys", Guid.NewGuid().ToString("N"));
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
    public void CreateSkinned_Gltf_RotationKeys_SurviveCook()
    {
        var source = SkinnedGltfFixture.WriteTwoBoneSkinned(_sourceDir, "rotbone", animationCount: 1, withRotation: true);

        var result = MeshCreator.CreateSkinned(source, _assetsRoot, "rotbone");
        result.Success.ShouldBeTrue(result.Error);

        using var animStream = File.OpenRead(Path.Combine(_assetsRoot, "models", "rotbone.anim3d"));
        var anim = Anim3dReader.Read(animStream);
        var clip = anim.Clips.Single(c => c.Name == "ClipA");
        var channel = clip.Channels.Single(c => c.RotationKeys.Count > 1);

        channel.RotationKeys.Count.ShouldBeGreaterThanOrEqualTo(3);

        for (var i = 1; i < channel.RotationKeys.Count; i++)
            channel.RotationKeys[i].Time.ShouldBeGreaterThan(
                channel.RotationKeys[i - 1].Time,
                $"rotation key times must be strictly increasing (key {i})");

        channel.RotationKeys[^1].Time.ShouldBe(1f, 0.01f, "last key must land at the authored 1s mark");

        foreach (var key in channel.RotationKeys)
            key.Value.Length().ShouldBe(1f, 0.01f, $"quat at t={key.Time} must be unit length");

        // Authored end pose: 90° about Z.
        var end = channel.RotationKeys[^1].Value;
        MathF.Abs(end.Z).ShouldBe(0.70710678f, 0.01f);
        MathF.Abs(end.W).ShouldBe(0.70710678f, 0.01f);
        MathF.Abs(end.X).ShouldBeLessThan(0.01f);
        MathF.Abs(end.Y).ShouldBeLessThan(0.01f);

        // Mid key: 45° about Z.
        var mid = channel.RotationKeys.Single(k => MathF.Abs(k.Time - 0.5f) < 0.01f);
        MathF.Abs(mid.Value.Z).ShouldBe(0.38268343f, 0.01f);
        MathF.Abs(mid.Value.W).ShouldBe(0.92387953f, 0.01f);
    }

    [Fact]
    public void CreateSkinned_Gltf_ScaleKeys_SurviveCook_AndScaleThePalette()
    {
        var source = SkinnedGltfFixture.WriteTwoBoneSkinned(_sourceDir, "scalebone", animationCount: 1, withScale: true);

        var result = MeshCreator.CreateSkinned(source, _assetsRoot, "scalebone");
        result.Success.ShouldBeTrue(result.Error);

        using var animStream = File.OpenRead(Path.Combine(_assetsRoot, "models", "scalebone.anim3d"));
        var anim = Anim3dReader.Read(animStream);
        var clip = anim.Clips.Single(c => c.Name == "ClipA");
        var channel = clip.Channels.Single(c => c.ScaleKeys.Count > 1);

        channel.ScaleKeys[0].Value.X.ShouldBe(1f, 0.01f);
        channel.ScaleKeys[^1].Time.ShouldBe(1f, 0.01f);
        channel.ScaleKeys[^1].Value.X.ShouldBe(2f, 0.01f);
        channel.ScaleKeys[^1].Value.Y.ShouldBe(2f, 0.01f);
        channel.ScaleKeys[^1].Value.Z.ShouldBe(2f, 0.01f);

        // End of clip: the scale delta from the first key (×2) must reach the palette.
        using var skelStream = File.OpenRead(Path.Combine(_assetsRoot, "models", "scalebone.skel"));
        var skeleton = SkeletonReader.Read(skelStream);
        var palette = new Matrix4x4[SkeletalPoseMath.MaxBones];
        SkeletalPoseMath.Evaluate(skeleton, clip, 1f, palette);

        var bone = (int)channel.BoneIndex;
        palette[bone].M11.ShouldBe(2f, 0.05f);
        palette[bone].M22.ShouldBe(2f, 0.05f);
        palette[bone].M33.ShouldBe(2f, 0.05f);
    }
}
