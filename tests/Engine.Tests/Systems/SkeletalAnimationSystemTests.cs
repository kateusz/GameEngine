using System.Numerics;
using System.Text.Json;
using ECS;
using Engine.Core;
using Engine.Renderer;
using Engine.Renderer.Meshes;
using Engine.Renderer.Models;
using Engine.Scene.Skeletal;
using NSubstitute;
using SceneComponents;
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

        SkeletalPlaybackUpdater.Tick(context, TimeSpan.FromSeconds(0.1), factory);

        playback.BonePalette[0].ShouldBe(Matrix4x4.Identity);
        playback.Time.ShouldBe(0f);
    }

    [Fact]
    public void Tick_Playing_AdvancesTimeAndEvaluates()
    {
        var (context, factory, playback) = CreatePlayingEntity(playing: true);

        SkeletalPlaybackUpdater.Tick(context, TimeSpan.FromSeconds(0.25), factory);

        playback.Time.ShouldBe(0.25f, 1e-5f);
        var moved = Vector3.Transform(Vector3.Zero, playback.BonePalette[0]);
        moved.X.ShouldBe(2.5f, 1e-3f);
    }

    [Fact]
    public void Tick_UnknownClip_IdentityPalette()
    {
        var (context, factory, playback) = CreatePlayingEntity(playing: true);
        playback.ClipName = "nope";

        SkeletalPlaybackUpdater.Tick(context, TimeSpan.FromSeconds(0.1), factory);

        playback.BonePalette[0].ShouldBe(Matrix4x4.Identity);
    }

    [Fact]
    public void Tick_MatchingAncestorPlayback_StampsRendererPaletteAndWorld()
    {
        var factory = SkinnedFactory();
        var (context, playback, child) = CreateRigWithChild("models/hero.mesh", "models/hero.mesh");
        var world = Matrix4x4.CreateTranslation(10, 0, 0);
        child.GetComponent<TransformComponent>().SetWorldTransform(world);

        SkeletalPlaybackUpdater.Tick(context, TimeSpan.Zero, factory);

        var renderer = child.GetComponent<ModelRendererComponent>();
        renderer.BonePalette.ShouldBeSameAs(playback.BonePalette);
        renderer.SkinningWorld.ShouldBe(world);
    }

    [Fact]
    public void Tick_PathMismatch_DoesNotStampSkinning()
    {
        var factory = SkinnedFactory();
        var (context, _, child) = CreateRigWithChild("models/hero.mesh", "models/crate.mesh");

        SkeletalPlaybackUpdater.Tick(context, TimeSpan.Zero, factory);

        var renderer = child.GetComponent<ModelRendererComponent>();
        renderer.BonePalette.ShouldBeNull();
        renderer.SkinningWorld.ShouldBe(Matrix4x4.Identity);
    }

    [Fact]
    public void Tick_SkinningWorld_UsesRendererEntityNotPlaybackAncestor()
    {
        var factory = SkinnedFactory();
        var (context, _, child) = CreateRigWithChild("models/hero.mesh", "models/hero.mesh");
        var parentWorld = Matrix4x4.CreateTranslation(0, 0.62f, 0);
        var rendererWorld = Matrix4x4.CreateFromAxisAngle(Vector3.UnitX, -MathF.PI / 2f)
            * Matrix4x4.CreateTranslation(0, -0.57f, 0);
        context.GetById(1).GetComponent<TransformComponent>().SetWorldTransform(parentWorld);
        child.GetComponent<TransformComponent>().SetWorldTransform(rendererWorld);

        SkeletalPlaybackUpdater.Tick(context, TimeSpan.Zero, factory);

        child.GetComponent<ModelRendererComponent>().SkinningWorld.ShouldBe(rendererWorld);
    }

    [Fact]
    public void Tick_PathUnchanged_RefreshesSkinningWorld()
    {
        var factory = SkinnedFactory();
        var (context, _, child) = CreateRigWithChild("models/hero.mesh", "models/hero.mesh");
        var rendererXf = child.GetComponent<TransformComponent>();
        rendererXf.SetWorldTransform(Matrix4x4.CreateTranslation(1, 0, 0));

        SkeletalPlaybackUpdater.Tick(context, TimeSpan.Zero, factory);
        rendererXf.SetWorldTransform(Matrix4x4.CreateTranslation(2, 0, 0));
        SkeletalPlaybackUpdater.Tick(context, TimeSpan.Zero, factory);

        child.GetComponent<ModelRendererComponent>().SkinningWorld.M41.ShouldBe(2f);
    }

    [Fact]
    public void Tick_ReparentAway_ClearsSkinningStamp()
    {
        var factory = SkinnedFactory();
        var (context, _, child) = CreateRigWithChild("models/hero.mesh", "models/hero.mesh");
        SkeletalPlaybackUpdater.Tick(context, TimeSpan.Zero, factory);
        child.GetComponent<ModelRendererComponent>().BonePalette.ShouldNotBeNull();

        child.GetComponent<ParentComponent>().ParentId = null;
        SkeletalPlaybackUpdater.Tick(context, TimeSpan.Zero, factory);

        var renderer = child.GetComponent<ModelRendererComponent>();
        renderer.BonePalette.ShouldBeNull();
        renderer.SkinningWorld.ShouldBe(Matrix4x4.Identity);
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

    [Fact]
    public void Serialize_ModelRenderer_OmitsPose()
    {
        var renderer = new ModelRendererComponent { ModelPath = "models/hero.mesh" };
        renderer.BonePalette = SkeletalPlaybackComponent.CreateIdentityPalette();
        renderer.SkinningWorld = Matrix4x4.CreateTranslation(3, 0, 0);

        var json = JsonSerializer.Serialize(renderer);
        json.ShouldContain("ModelPath");
        json.ShouldNotContain("BonePalette");
        json.ShouldNotContain("SkinningWorld");
    }

    private (Context Context, IModelFactory Factory, SkeletalPlaybackComponent Playback) CreatePlayingEntity(bool playing)
    {
        var factory = SkinnedFactory();
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

    private static (Context Context, SkeletalPlaybackComponent Playback, Entity Child)
        CreateRigWithChild(string playbackPath, string rendererPath)
    {
        var context = new Context();
        var parent = Entity.Create(1, "rig");
        parent.AddComponent(new TransformComponent());
        var playback = new SkeletalPlaybackComponent { MeshPath = playbackPath };
        parent.AddComponent(playback);
        context.Register(parent);

        var child = Entity.Create(2, "mesh");
        child.AddComponent(new TransformComponent());
        child.AddComponent(new ParentComponent(parent.Id));
        child.AddComponent(new ModelRendererComponent { ModelPath = rendererPath });
        context.Register(child);
        return (context, playback, child);
    }

    private static IModelFactory SkinnedFactory()
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
        return factory;
    }
}
