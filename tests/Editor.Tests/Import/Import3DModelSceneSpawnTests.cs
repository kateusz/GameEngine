using System.Numerics;
using ECS;
using ECS.Systems;
using Editor.Features.Import;
using Engine.Core.Window;
using Engine.Renderer;
using Engine.Scene;
using Engine.Scene.Systems;
using NSubstitute;
using SceneComponents;
using SceneComponents.Rendering;
using Shouldly;

namespace Editor.Tests.Import;

public class Import3DModelSceneSpawnTests
{
    private static IScene CreateScene()
    {
        var systemManagerFactory = Substitute.For<ISystemManagerFactory>();
        systemManagerFactory.Create(Arg.Any<IContext>()).Returns(_ => new SceneBuildResult(
            Substitute.For<ISystemManager>(),
            new PhysicsRuntimeBodyStore(),
            new PhysicsContactQueue(),
            new ScriptRuntimeStore(),
            null!));

        return new SceneFactory(systemManagerFactory, Substitute.For<IPointerSurface>())
            .Create("test-scene", "test-scene");
    }

    [Fact]
    public void SpawnHierarchy_CreatesParentAndChildrenWithModelPathsAndParentLinks()
    {
        using var scene = CreateScene();
        var parts = new List<MeshCreator.SplitPart>
        {
            new("Door", "models/house.mesh", 0, 1, new Vector3(1, 0, 0), Vector3.Zero, Vector3.One),
            new("Roof", "models/house.mesh", 1, 2, new Vector3(0, 2, 0), Vector3.Zero, Vector3.One),
        };

        var note = Import3DModelBatch.SpawnHierarchy(scene, "house", parts);

        note.ShouldContain("house");
        note.ShouldContain("2");

        var parent = scene.Entities.Single(e => e.Name == "house");
        parent.HasComponent<ModelRendererComponent>().ShouldBeFalse();

        var children = scene.GetChildren(parent).ToList();
        children.Count.ShouldBe(2);

        var door = children.Single(c => c.Name == "Door");
        var doorRenderer = door.GetComponent<ModelRendererComponent>();
        doorRenderer.ModelPath.ShouldBe("models/house.mesh");
        doorRenderer.SubmeshStart.ShouldBe(0);
        doorRenderer.SubmeshCount.ShouldBe(1);
        door.GetComponent<TransformComponent>().Translation.ShouldBe(new Vector3(1, 0, 0));
        scene.GetParent(door)!.Id.ShouldBe(parent.Id);

        var roof = children.Single(c => c.Name == "Roof");
        var roofRenderer = roof.GetComponent<ModelRendererComponent>();
        roofRenderer.ModelPath.ShouldBe("models/house.mesh");
        roofRenderer.SubmeshStart.ShouldBe(1);
        roofRenderer.SubmeshCount.ShouldBe(2);
    }

    [Fact]
    public void SpawnHierarchy_Skinned_AttachesPlaybackOnParentWithCompanionPaths()
    {
        using var scene = CreateScene();
        var parts = new List<MeshCreator.SplitPart>
        {
            new("Body", "models/hero.mesh", 0, 1, Vector3.Zero, Vector3.Zero, Vector3.One),
        };

        Import3DModelBatch.SpawnHierarchy(
            scene, "hero", parts,
            skeletonRelativePath: "models/hero.skel",
            clipRelativePath: "models/hero.anim3d");

        var parent = scene.Entities.Single(e => e.Name == "hero");
        parent.HasComponent<ModelRendererComponent>().ShouldBeFalse();
        var playback = parent.GetComponent<SkeletalPlaybackComponent>();
        playback.SkeletonPath.ShouldBe("models/hero.skel");
        playback.ClipPath.ShouldBe("models/hero.anim3d");

        var child = scene.GetChildren(parent).ShouldHaveSingleItem();
        child.HasComponent<SkeletalPlaybackComponent>().ShouldBeFalse();
        child.GetComponent<ModelRendererComponent>().ModelPath.ShouldBe("models/hero.mesh");
    }

    [Fact]
    public void SpawnHierarchy_BoneFree_DoesNotAttachPlayback()
    {
        using var scene = CreateScene();
        var parts = new List<MeshCreator.SplitPart>
        {
            new("Prop", "models/crate.mesh", 0, 1, Vector3.Zero, Vector3.Zero, Vector3.One),
        };

        Import3DModelBatch.SpawnHierarchy(scene, "crate", parts);

        var parent = scene.Entities.Single(e => e.Name == "crate");
        parent.HasComponent<SkeletalPlaybackComponent>().ShouldBeFalse();
        scene.GetChildren(parent).ShouldHaveSingleItem()
            .HasComponent<SkeletalPlaybackComponent>().ShouldBeFalse();
    }
}
