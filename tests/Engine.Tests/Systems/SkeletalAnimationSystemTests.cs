using System.Numerics;
using ECS;
using Engine.Core;
using Engine.Platform;
using Engine.Renderer;
using Engine.Scene.Systems;
using NSubstitute;
using SceneComponents.Rendering;
using Shouldly;

namespace Engine.Tests.Systems;

[Trait("Category", "Unit")]
public class SkeletalAnimationSystemTests : IDisposable
{
    private readonly string _dir;

    public SkeletalAnimationSystemTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "skel-anim-sys-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        var project = Substitute.For<IProjectContext>();
        project.AssetsPath.Returns(_dir);
        PathBuilder.UseProjectContext(project);
    }

    public void Dispose()
    {
        PathBuilder.UseProjectContext(Substitute.For<IProjectContext>());
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void OnUpdate_AdvancesTime_Loops_FreezesWhenNotPlaying_EmptyClipNameUsesFirstClip()
    {
        var (skelPath, animPath) = WriteTwoClipAssets();
        var context = new Context();
        var entity = Entity.Create(1, "hero");
        var playback = new SkeletalPlaybackComponent
        {
            SkeletonPath = skelPath,
            ClipPath = animPath,
            ClipName = "",
            Time = 0.8f,
            Speed = 1f,
            Loop = true,
            Playing = true
        };
        entity.AddComponent(playback);
        context.Register(entity);

        var system = new SkeletalAnimationSystem(context, new SkeletonFactory(), new Anim3dFactory());

        system.OnUpdate(TimeSpan.FromSeconds(0.5));
        playback.Time.ShouldBe(0.3f, 1e-5f);

        // ClipA translates root; palette should reflect motion at t=0.3s.
        playback.BonePalette[0].ShouldNotBe(Matrix4x4.Identity);
        playback.BonePalette[1].ShouldNotBe(Matrix4x4.Identity);

        playback.Playing = false;
        var frozen = playback.Time;
        system.OnUpdate(TimeSpan.FromSeconds(1));
        playback.Time.ShouldBe(frozen);
        for (var i = 0; i < 100; i++)
            playback.BonePalette[i].ShouldBe(Matrix4x4.Identity);
    }

    [Fact]
    public void OnUpdate_NullClipName_UsesFirstClip()
    {
        var (skelPath, animPath) = WriteTwoClipAssets();
        var context = new Context();
        var entity = Entity.Create(1, "hero");
        var playback = new SkeletalPlaybackComponent
        {
            SkeletonPath = skelPath,
            ClipPath = animPath,
            ClipName = null,
            Time = 0f,
            Speed = 0f,
            Loop = true,
            Playing = true
        };
        entity.AddComponent(playback);
        context.Register(entity);

        new SkeletalAnimationSystem(context, new SkeletonFactory(), new Anim3dFactory())
            .OnUpdate(TimeSpan.Zero);

        // First clip (ClipA) at bind reference; bone palette is identity at t=0.
        playback.BonePalette[0].ShouldBe(Matrix4x4.Identity);
        playback.BonePalette[1].ShouldBe(Matrix4x4.Identity);
    }

    [Fact]
    public void OnUpdate_UnknownClipName_FillsIdentityPalette()
    {
        var (skelPath, animPath) = WriteTwoClipAssets();
        var context = new Context();
        var entity = Entity.Create(1, "hero");
        var playback = new SkeletalPlaybackComponent
        {
            SkeletonPath = skelPath,
            ClipPath = animPath,
            ClipName = "MissingClip",
            Time = 0f,
            Speed = 0f,
            Loop = true,
            Playing = true
        };
        entity.AddComponent(playback);
        context.Register(entity);

        new SkeletalAnimationSystem(context, new SkeletonFactory(), new Anim3dFactory())
            .OnUpdate(TimeSpan.Zero);

        for (var i = 0; i < 100; i++)
            playback.BonePalette[i].ShouldBe(Matrix4x4.Identity);
    }

    [Fact]
    public void TwoEntities_SharedPaths_DistinctPalettesAtDifferentTimes()
    {
        var (skelPath, animPath) = WriteTwoClipAssets();
        var context = new Context();

        var a = Entity.Create(1, "a");
        var playbackA = new SkeletalPlaybackComponent
        {
            SkeletonPath = skelPath,
            ClipPath = animPath,
            ClipName = "ClipA",
            Time = 0f,
            Speed = 0f,
            Loop = true,
            Playing = true
        };
        a.AddComponent(playbackA);
        context.Register(a);

        var b = Entity.Create(2, "b");
        var playbackB = new SkeletalPlaybackComponent
        {
            SkeletonPath = skelPath,
            ClipPath = animPath,
            ClipName = "ClipA",
            Time = 0.5f,
            Speed = 0f,
            Loop = true,
            Playing = true
        };
        b.AddComponent(playbackB);
        context.Register(b);

        var system = new SkeletalAnimationSystem(context, new SkeletonFactory(), new Anim3dFactory());
        system.OnUpdate(TimeSpan.Zero);

        playbackA.BonePalette.ShouldNotBeSameAs(playbackB.BonePalette);
        playbackA.BonePalette[0].ShouldNotBe(playbackB.BonePalette[0]);
    }

    private (string SkelPath, string AnimPath) WriteTwoClipAssets()
    {
        var skeleton = new SkeletonAsset(
        [
            new SkeletonBone("root", -1, Matrix4x4.Identity),
            new SkeletonBone("child", 0, Matrix4x4.Identity)
        ]);

        var clipA = new Anim3dClip(
            "ClipA",
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

        var clipB = new Anim3dClip(
            "ClipB",
            1f,
            [
                new Anim3dChannel(
                    1,
                    [new Anim3dVec3Key(0f, new Vector3(0, 5, 0))],
                    [new Anim3dQuatKey(0f, Quaternion.Identity)],
                    [new Anim3dVec3Key(0f, Vector3.One)])
            ]);

        var skelPath = Path.Combine(_dir, "hero.skel");
        var animPath = Path.Combine(_dir, "hero.anim3d");
        using (var fs = File.Create(skelPath))
            SkeletonWriter.Write(fs, skeleton);
        using (var fs = File.Create(animPath))
            Anim3dWriter.Write(fs, new Anim3dAsset([clipA, clipB]));

        return (skelPath, animPath);
    }
}
