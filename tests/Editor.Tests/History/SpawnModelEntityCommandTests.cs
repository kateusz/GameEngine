using ECS;
using ECS.Systems;
using Editor.Features.History.Commands;
using Engine.Core.Window;
using Engine.Renderer.Models;
using Engine.Scene;
using Engine.Scene.Systems;
using NSubstitute;
using SceneComponents;
using Scripting;
using SceneComponents.Rendering;
using Shouldly;

namespace Editor.Tests.History;

public class SpawnModelEntityCommandTests
{
    private static IScene CreateScene()
    {
        var systemsFactory = Substitute.For<ISceneSystemsFactory>();
        systemsFactory.PopulateSystemManager(
                Arg.Any<SystemManager>(),
                Arg.Any<Context>(),
                Arg.Any<PhysicsRuntimeBodyStore>(),
                Arg.Any<PhysicsContactQueue>())
            .Returns(Substitute.For<IPhysicsQueries>());

        return new SceneFactory(systemsFactory, Substitute.For<IPointerSurface>())
            .Create("test-scene");
    }

    private static Model ModelWithGraph(ModelSceneNode graph) =>
        new("crate.glb", [], graph);

    [Fact]
    public void Execute_CreatesEntityWithDroppedModel()
    {
        using var scene = CreateScene();
        var model = ModelWithGraph(new ModelSceneNode("Crate", [0], []));

        var command = new SpawnModelEntityCommand(scene, "Crate", model, "models/crate.glb");
        command.Execute().ShouldBeTrue();

        var entity = scene.Entities.Single();
        entity.Name.ShouldBe("Crate");
        entity.HasComponent<TransformComponent>().ShouldBeTrue();
        entity.GetComponent<ModelRendererComponent>().ModelPath.ShouldBe("models/crate.glb");
        command.EntityId.ShouldBe(entity.Id);
    }

    [Fact]
    public void Execute_MultiMeshModel_UnpacksChildren()
    {
        using var scene = CreateScene();
        var graph = new ModelSceneNode("Room", [],
        [
            new ModelSceneNode("Chair", [0], []),
            new ModelSceneNode("Table", [1], [])
        ]);

        var command = new SpawnModelEntityCommand(scene, "Room", ModelWithGraph(graph), "models/room.glb");
        command.Execute();

        var root = scene.Entities.Single(e => e.Name == "Room");
        root.GetComponent<ModelRendererComponent>().SuppressDraw.ShouldBeTrue();
        scene.GetChildren(root).Select(e => e.Name).OrderBy(n => n).ShouldBe(["Chair", "Table"]);
    }

    [Fact]
    public void Undo_RemovesSpawnedEntity_RedoRecreatesIt()
    {
        using var scene = CreateScene();
        var model = ModelWithGraph(new ModelSceneNode("Crate", [0], []));
        var command = new SpawnModelEntityCommand(scene, "Crate", model, "models/crate.glb");

        command.Execute();
        command.Undo();
        scene.Entities.ShouldBeEmpty();

        command.Execute();
        var entity = scene.Entities.Single();
        entity.Name.ShouldBe("Crate");
        entity.GetComponent<ModelRendererComponent>().ModelPath.ShouldBe("models/crate.glb");
        command.EntityId.ShouldBe(entity.Id);
    }
}
