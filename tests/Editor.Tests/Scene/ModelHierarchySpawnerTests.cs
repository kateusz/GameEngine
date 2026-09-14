using System.Numerics;
using ECS;
using ECS.Systems;
using Editor.Features.Scene;
using Engine.Renderer.Models;
using Engine.Scene;
using Engine.Scene.Systems;
using NSubstitute;
using SceneComponents;
using SceneComponents.Rendering;
using Scripting;
using Shouldly;
using EngineScene = Engine.Scene.Scene;

namespace Editor.Tests.Scene;

public class ModelHierarchySpawnerTests
{
    private readonly ISystemManager _systemManager = Substitute.For<ISystemManager>();

    private EngineScene CreateScene() =>
        new("test-scene", "test-scene", new Context(),
            _systemManager, new PhysicsRuntimeBodyStore(), new PhysicsContactQueue(),
            new ScriptRuntimeStore(), null!, NullCameraQueries.Instance);

    private static ModelSceneNode Node(
        string name,
        IReadOnlyList<int> meshIndices,
        params ModelSceneNode[] children) =>
        new(name, meshIndices, children);

    private static ModelSceneNode Node(
        string name,
        IReadOnlyList<int> meshIndices,
        Matrix4x4 localTransform,
        params ModelSceneNode[] children) =>
        new(name, meshIndices, children, localTransform);

    [Fact]
    public void SpawnChildren_SingleMeshGraph_DoesNotCreateChildren()
    {
        using var scene = CreateScene();
        var root = scene.CreateEntity("Root");
        root.AddComponent(new TransformComponent());
        root.AddComponent(new ModelRendererComponent());

        ModelHierarchySpawner.SpawnChildren(scene, root, Node("root", [0]), "models/room.fbx", System.Numerics.Vector4.One);

        scene.GetChildren(root).Count.ShouldBe(0);
    }

    [Fact]
    public void SpawnChildren_RoomWithChairAndTable_CreatesSelectableChildren()
    {
        using var scene = CreateScene();
        var root = scene.CreateEntity("Room");
        root.AddComponent(new TransformComponent());
        root.AddComponent(new ModelRendererComponent());

        var graph = Node("Room", [], Node("Chair", [0]), Node("Table", [1]));
        ModelHierarchySpawner.SpawnChildren(scene, root, graph, "models/room.fbx", System.Numerics.Vector4.One);

        var children = scene.GetChildren(root).Select(e => e.Name).OrderBy(n => n).ToArray();
        children.ShouldBe(["Chair", "Table"]);

        var chairEntity = scene.GetChildren(root).Single(e => e.Name == "Chair");
        chairEntity.HasComponent<TransformComponent>().ShouldBeTrue();
        chairEntity.TryGetComponent<ModelRendererComponent>(out var chairRenderer).ShouldBeTrue();
        chairRenderer!.MeshIndex.ShouldBe(0);
        chairRenderer.ModelPath.ShouldBe("models/room.fbx");

        var tableEntity = scene.GetChildren(root).Single(e => e.Name == "Table");
        tableEntity.TryGetComponent<ModelRendererComponent>(out var tableRenderer).ShouldBeTrue();
        tableRenderer!.MeshIndex.ShouldBe(1);
    }

    [Fact]
    public void SpawnChildren_NodeWithMultipleMeshes_CreatesMeshChildren()
    {
        using var scene = CreateScene();
        var root = scene.CreateEntity("Root");
        root.AddComponent(new TransformComponent());
        root.AddComponent(new ModelRendererComponent());

        ModelHierarchySpawner.SpawnChildren(scene, root, Node("Cabinet", [0, 1]), "models/cabinet.fbx", System.Numerics.Vector4.One);

        var children = scene.GetChildren(root).Select(e => e.Name).OrderBy(n => n).ToArray();
        children.ShouldBe(["Cabinet_mesh0", "Cabinet_mesh1"]);
        foreach (var child in scene.GetChildren(root))
            child.HasComponent<TransformComponent>().ShouldBeTrue();
    }

    [Fact]
    public void SpawnChildren_AppliesNodeLocalTransform()
    {
        using var scene = CreateScene();
        var root = scene.CreateEntity("Room");
        root.AddComponent(new TransformComponent());
        root.AddComponent(new ModelRendererComponent());

        var graph = Node("Room", [],
            localTransform: Matrix4x4.CreateTranslation(1, 2, 3),
            Node("Chair", [0], Matrix4x4.CreateTranslation(10, 0, 0)),
            Node("Table", [1], Matrix4x4.CreateTranslation(0, 0, 5)));

        ModelHierarchySpawner.SpawnChildren(scene, root, graph, "models/room.fbx", Vector4.One);

        var chair = scene.GetChildren(root).Single(e => e.Name == "Chair");
        chair.TryGetComponent<TransformComponent>(out var chairTransform).ShouldBeTrue();
        chairTransform!.Translation.ShouldBe(new Vector3(10, 0, 0));

        var table = scene.GetChildren(root).Single(e => e.Name == "Table");
        table.TryGetComponent<TransformComponent>(out var tableTransform).ShouldBeTrue();
        tableTransform!.Translation.ShouldBe(new Vector3(0, 0, 5));
    }
}
