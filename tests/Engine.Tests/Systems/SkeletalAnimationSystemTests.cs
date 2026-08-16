using System.Numerics;
using System.Text.Json;
using ECS;
using Engine.Core;
using Engine.Renderer;
using Engine.Scene.Skeletal;
using NSubstitute;
using SceneComponents.Rendering;
using Shouldly;

namespace Engine.Tests.Systems;

[Trait("Category", "Unit")]
[Collection("PathBuilder")]
public class SkeletalAnimationSystemTests : IDisposable
{
    private readonly string _assetsRoot;

    public SkeletalAnimationSystemTests()
    {
        _assetsRoot = Path.Combine(Path.GetTempPath(), "GameEngine-SkeletalTests", Guid.NewGuid().ToString("N"), "assets");
        Directory.CreateDirectory(_assetsRoot);
        var context = Substitute.For<IProjectContext>();
        context.AssetsPath.Returns(_assetsRoot);
        PathBuilder.UseProjectContext(context);
    }

    public void Dispose()
    {
        PathBuilder.UseProjectContext(Substitute.For<IProjectContext>());
        try
        {
            var root = Directory.GetParent(_assetsRoot)?.FullName;
            if (root is not null && Directory.Exists(root))
                Directory.Delete(root, true);
        }
        catch
        {
            // ponytail: temp cleanup best-effort
        }
    }

    [Fact]
    public void MaxBones_MatchesPoseMathPalette()
    {
        SkeletalPlaybackComponent.MaxBones.ShouldBe(SkeletalLimits.MaxBones);
        SkeletalPoseMath.CreateIdentityPalette().Length.ShouldBe(SkeletalLimits.MaxBones);
    }

    [Fact]
    public void Tick_NotPlaying_WritesIdentityPalette()
    {
        var (context, factory, playback) = CreatePlayingEntity(playing: false);
        playback.BonePalette[0] = Matrix4x4.CreateTranslation(9, 0, 0);

        SkeletalPlaybackUpdater.Tick(context, factory, TimeSpan.FromSeconds(0.1));

        playback.BonePalette[0].ShouldBe(Matrix4x4.Identity);
        playback.Time.ShouldBe(0f);
    }

    [Fact]
    public void Tick_Playing_AdvancesTimeAndEvaluates()
    {
        var (context, factory, playback) = CreatePlayingEntity(playing: true);

        SkeletalPlaybackUpdater.Tick(context, factory, TimeSpan.FromSeconds(0.25));

        playback.Time.ShouldBe(0.25f, 1e-5f);
        var moved = Vector3.Transform(Vector3.Zero, playback.BonePalette[0]);
        moved.X.ShouldBe(2.5f, 1e-3f);
    }

    [Fact]
    public void Tick_UnknownClip_IdentityPalette()
    {
        var (context, factory, playback) = CreatePlayingEntity(playing: true);
        playback.ClipName = "nope";

        SkeletalPlaybackUpdater.Tick(context, factory, TimeSpan.FromSeconds(0.1));

        playback.BonePalette[0].ShouldBe(Matrix4x4.Identity);
    }

    [Fact]
    public void Clone_DoesNotCopyPalette()
    {
        var playback = new SkeletalPlaybackComponent { Playing = true, MeshPath = "models/a.mesh" };
        playback.BonePalette[0] = Matrix4x4.CreateTranslation(1, 2, 3);

        var clone = (SkeletalPlaybackComponent)playback.Clone();
        clone.Playing.ShouldBeTrue();
        clone.MeshPath.ShouldBe("models/a.mesh");
        clone.BonePalette[0].ShouldBe(Matrix4x4.Identity);
    }

    [Fact]
    public void Serialize_OmitsBonePalette()
    {
        var json = JsonSerializer.Serialize(new SkeletalPlaybackComponent { MeshPath = "models/a.mesh" });
        json.ShouldContain("MeshPath");
        json.ShouldNotContain("BonePalette");
    }

    private (Context Context, IModelFactory Factory, SkeletalPlaybackComponent Playback) CreatePlayingEntity(bool playing)
    {
        var model = new Model(
            [new ModelSubmesh(new Mesh("m"), new MeshMaterial())],
            [new SkeletonBone("root", -1, Matrix4x4.Identity)],
            [new AnimationClip("walk", 1f,
            [
                new BoneChannel(
                    0,
                    [new VectorKey(0f, Vector3.Zero), new VectorKey(1f, new Vector3(10, 0, 0))],
                    [new RotationKey(0f, Quaternion.Identity)],
                    [new VectorKey(0f, Vector3.One)])
            ])]);

        var factory = Substitute.For<IModelFactory>();
        factory.Create(Arg.Any<string>()).Returns(model);

        var context = new Context();
        var entity = Entity.Create(1, "rig");
        var playback = new SkeletalPlaybackComponent
        {
            MeshPath = "models/hero.mesh",
            Playing = playing,
            Loop = true,
            Speed = 1f
        };
        entity.AddComponent(playback);
        context.Register(entity);
        return (context, factory, playback);
    }
}
