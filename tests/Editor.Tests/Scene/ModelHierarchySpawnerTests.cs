using System.Numerics;
using ECS;
using ECS.Systems;
using Editor.Features.Scene;
using Engine.Renderer.Models;
using Engine.Scene;
using Engine.Scene.Systems;
using SceneComponents;
using SceneComponents.Lighting;
using SceneComponents.Rendering;
using Scripting;
using Shouldly;
using EngineScene = Engine.Scene.Scene;

namespace Editor.Tests.Scene;

public class ModelHierarchySpawnerTests
{
    private EngineScene CreateScene() =>
        new("test-scene", new Context(),
            new SystemManager(), new PhysicsRuntimeBodyStore(), new PhysicsContactQueue(),
            null!, NullCameraQueries.Instance);

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

    [Fact]
    public void SpawnChildren_EmptyLightNode_CreatesPointLightWithoutRenderer()
    {
        using var scene = CreateScene();
        var root = scene.CreateEntity("Room");
        root.AddComponent(new TransformComponent());
        root.AddComponent(new ModelRendererComponent());

        var torch = new ModelSceneNode("Torch", [], [], Matrix4x4.CreateTranslation(2f, 0f, 0f),
            new ImportedPointLight(Vector4.One, 1f, 6f));
        ModelHierarchySpawner.SpawnChildren(scene, root, new ModelSceneNode("Room", [], [torch]), "models/room.fbx", Vector4.One);

        var lamp = scene.GetChildren(root).Single();
        lamp.Name.ShouldBe("Torch");
        lamp.HasComponent<TransformComponent>().ShouldBeTrue();
        lamp.HasComponent<ModelRendererComponent>().ShouldBeFalse();
        lamp.GetComponent<PointLightComponent>().Range.ShouldBe(6f);
    }

    [Fact]
    public void SpawnChildren_MeshNodeWithPointLight_KeepsBothComponents()
    {
        using var scene = CreateScene();
        var root = scene.CreateEntity("Room");
        root.AddComponent(new TransformComponent());
        root.AddComponent(new ModelRendererComponent());

        var torch = new ModelSceneNode("Torch", [0], [], Matrix4x4.Identity,
            new ImportedPointLight(new Vector4(1f, 0.5f, 0.2f, 1f), 1.5f, 8f));
        ModelHierarchySpawner.SpawnChildren(scene, root, new ModelSceneNode("Room", [], [torch]), "models/room.fbx", Vector4.One);

        var lamp = scene.GetChildren(root).Single();
        lamp.HasComponent<ModelRendererComponent>().ShouldBeTrue();
        var light = lamp.GetComponent<PointLightComponent>();
        light.Intensity.ShouldBe(1.5f);
        light.Range.ShouldBe(8f);
        light.Color.ShouldBe(new Vector4(1f, 0.5f, 0.2f, 1f));
    }

    [Fact]
    public void SpawnChildren_LightOnGraphRoot_BecomesChild()
    {
        using var scene = CreateScene();
        var root = scene.CreateEntity("Root");
        root.AddComponent(new TransformComponent());
        root.AddComponent(new ModelRendererComponent());

        var graph = new ModelSceneNode("Root", [0], [], Matrix4x4.Identity,
            new ImportedPointLight(Vector4.One, 1f, 6f));
        ModelHierarchySpawner.SpawnChildren(scene, root, graph, "models/room.fbx", Vector4.One);

        root.HasComponent<PointLightComponent>().ShouldBeFalse();
        scene.GetChildren(root).Single().HasComponent<PointLightComponent>().ShouldBeTrue();
    }

    [Fact]
    public void SpawnPackedLights_ChildUnderTranslatedRoot_KeepsFileLocalTranslation()
    {
        using var scene = CreateScene();
        var root = scene.CreateEntity("Root");
        root.AddComponent(new TransformComponent());
        root.AddComponent(new ModelRendererComponent());

        var torch = new ModelSceneNode("Torch", [], [], Matrix4x4.CreateTranslation(4f, 0f, 0f),
            new ImportedPointLight(Vector4.One, 1f, 6f));
        var graph = new ModelSceneNode("Root", [0], [torch], Matrix4x4.CreateTranslation(1f, 0f, 0f));

        ModelHierarchySpawner.SpawnPackedLights(scene, root, graph);

        var lamp = scene.GetChildren(root).Single(e => e.Name == "Torch");
        lamp.GetComponent<TransformComponent>().Translation.ShouldBe(new Vector3(4f, 0f, 0f));
    }
}
