using System.Numerics;
using ECS;
using ECS.Systems;
using Engine.Scene;
using Engine.Scene.Serializer;
using Engine.Scene.Systems;
using NSubstitute;
using SceneComponents;
using Scripting;
using Shouldly;
using EngineScene = Engine.Scene.Scene;

namespace Engine.Tests.Scene;

public class EntityHierarchyTests
{
    private readonly ISystemManager _systemManager = Substitute.For<ISystemManager>();

    private EngineScene CreateScene() =>
        new("test-scene", "test-scene", new Context(),
            _systemManager, new PhysicsRuntimeBodyStore(), new PhysicsContactQueue(),
            new ScriptRuntimeStore(), null!, NullCameraQueries.Instance);

    private static Entity CreateWithTransform(EngineScene scene, string name, Vector3 translation)
    {
        var entity = scene.CreateEntity(name);
        entity.AddComponent(new TransformComponent(translation, Vector3.Zero, Vector3.One));
        return entity;
    }

    [Fact]
    public void SetParent_UpdatesIndexAndParentComponent()
    {
        using var scene = CreateScene();
        var parent = CreateWithTransform(scene, "parent", Vector3.Zero);
        var child = CreateWithTransform(scene, "child", Vector3.UnitX);

        scene.SetParent(child, parent).ShouldBeTrue();

        scene.GetParent(child)!.Id.ShouldBe(parent.Id);
        scene.GetChildren(parent).Select(e => e.Id).ShouldBe([child.Id]);
        child.GetComponent<ParentComponent>().ParentId.ShouldBe(parent.Id);
        scene.GetRootEntities().Select(e => e.Id).ShouldBe([parent.Id]);
    }

    [Fact]
    public void SetParent_Null_DetachesToRoot()
    {
        using var scene = CreateScene();
        var parent = CreateWithTransform(scene, "parent", Vector3.Zero);
        var child = CreateWithTransform(scene, "child", Vector3.UnitX);
        scene.SetParent(child, parent);

        scene.SetParent(child, null).ShouldBeTrue();

        scene.GetParent(child).ShouldBeNull();
        scene.GetChildren(parent).ShouldBeEmpty();
        child.HasComponent<ParentComponent>().ShouldBeFalse();
    }

    [Fact]
    public void SetParent_Cycle_IsRejected()
    {
        using var scene = CreateScene();
        var a = CreateWithTransform(scene, "a", Vector3.Zero);
        var b = CreateWithTransform(scene, "b", Vector3.UnitX);
        var c = CreateWithTransform(scene, "c", Vector3.UnitY);
        scene.SetParent(b, a);
        scene.SetParent(c, b);

        scene.SetParent(a, c).ShouldBeFalse();

        scene.GetParent(a).ShouldBeNull();
        scene.GetParent(c)!.Id.ShouldBe(b.Id);
    }

    [Fact]
    public void WorldTransform_Root_EqualsLocal()
    {
        using var scene = CreateScene();
        var root = CreateWithTransform(scene, "root", new Vector3(3, 4, 5));

        scene.UpdateWorldTransforms();

        var world = root.GetComponent<TransformComponent>().GetWorldTransform();
        var local = root.GetComponent<TransformComponent>().GetTransform();
        world.ShouldBe(local);
    }

    [Fact]
    public void WorldTransform_Child_ComposesParentThenLocal()
    {
        using var scene = CreateScene();
        var parent = scene.CreateEntity("parent");
        parent.AddComponent(new TransformComponent(
            new Vector3(10, 0, 0),
            new Vector3(0, 0, MathF.PI / 2f),
            new Vector3(2, 1, 1)));
        var child = CreateWithTransform(scene, "child", new Vector3(1, 2, 0));
        scene.SetParent(child, parent);

        scene.UpdateWorldTransforms();

        var parentLocal = parent.GetComponent<TransformComponent>().GetTransform();
        var childLocal = child.GetComponent<TransformComponent>().GetTransform();
        var expected = childLocal * parentLocal;
        var reversed = parentLocal * childLocal;
        var world = child.GetComponent<TransformComponent>().GetWorldTransform();

        world.M41.ShouldBe(expected.M41, 0.0001f);
        world.M42.ShouldBe(expected.M42, 0.0001f);
        world.M43.ShouldBe(expected.M43, 0.0001f);
        // Non-commutative: reversed order must differ when parent has rotation/scale
        (MathF.Abs(reversed.M41 - expected.M41) + MathF.Abs(reversed.M42 - expected.M42))
            .ShouldBeGreaterThan(0.01f);
        world.Translation.X.ShouldBe(expected.Translation.X, 0.0001f);
        world.Translation.Y.ShouldBe(expected.Translation.Y, 0.0001f);
        world.Translation.Z.ShouldBe(expected.Translation.Z, 0.0001f);
    }

    [Fact]
    public void DirtyPropagation_ParentMove_UpdatesChildWorld()
    {
        using var scene = CreateScene();
        var parent = CreateWithTransform(scene, "parent", Vector3.Zero);
        var child = CreateWithTransform(scene, "child", new Vector3(1, 0, 0));
        scene.SetParent(child, parent);
        scene.UpdateWorldTransforms();

        parent.GetComponent<TransformComponent>().Translation = new Vector3(5, 0, 0);
        scene.UpdateWorldTransforms();

        var pos = child.GetComponent<TransformComponent>().GetWorldTransform().Translation;
        pos.X.ShouldBe(6f, 0.0001f);
        pos.Y.ShouldBe(0f, 0.0001f);
        pos.Z.ShouldBe(0f, 0.0001f);
    }

    [Fact]
    public void DestroyEntity_CascadesToDescendants()
    {
        using var scene = CreateScene();
        var root = CreateWithTransform(scene, "root", Vector3.Zero);
        var mid = CreateWithTransform(scene, "mid", Vector3.Zero);
        var leaf1 = CreateWithTransform(scene, "leaf1", Vector3.Zero);
        var leaf2 = CreateWithTransform(scene, "leaf2", Vector3.Zero);
        scene.SetParent(mid, root);
        scene.SetParent(leaf1, mid);
        scene.SetParent(leaf2, mid);

        scene.DestroyEntity(root);

        scene.Entities.ShouldBeEmpty();
    }

    [Fact]
    public void DuplicateEntity_ClonesSubtreeWithRemappedParents()
    {
        using var scene = CreateScene();
        var root = CreateWithTransform(scene, "root", Vector3.Zero);
        var child = CreateWithTransform(scene, "child", Vector3.UnitX);
        scene.SetParent(child, root);

        var cloneRoot = scene.DuplicateEntity(root);

        cloneRoot.Id.ShouldNotBe(root.Id);
        var cloneChildren = scene.GetChildren(cloneRoot);
        cloneChildren.Count.ShouldBe(1);
        cloneChildren[0].Id.ShouldNotBe(child.Id);
        cloneChildren[0].Name.ShouldBe("child");
        scene.GetParent(cloneChildren[0])!.Id.ShouldBe(cloneRoot.Id);
    }

    [Fact]
    public void SceneRoundTrip_PreservesHierarchy()
    {
        var registry = new ComponentSerializerRegistry();
        var options = new SerializerOptions();
        var serializer = new SceneSerializer(registry, options);
        var path = Path.Combine(Path.GetTempPath(), $"hierarchy-{Guid.NewGuid():N}.scene");

        try
        {
            using (var scene = CreateScene())
            {
                var a = CreateWithTransform(scene, "a", Vector3.Zero);
                var b = CreateWithTransform(scene, "b", Vector3.UnitX);
                var c = CreateWithTransform(scene, "c", Vector3.UnitY);
                scene.SetParent(b, a);
                scene.SetParent(c, b);
                serializer.Serialize(scene, path);
            }

            using var loaded = CreateScene();
            serializer.Deserialize(loaded, path);

            var a2 = loaded.Entities.Single(e => e.Name == "a");
            var b2 = loaded.Entities.Single(e => e.Name == "b");
            var c2 = loaded.Entities.Single(e => e.Name == "c");
            loaded.GetParent(b2)!.Id.ShouldBe(a2.Id);
            loaded.GetParent(c2)!.Id.ShouldBe(b2.Id);
            loaded.GetRootEntities().Select(e => e.Name).ShouldBe(["a"]);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void RebuildHierarchyIndex_OrphanParentId_DetachesToRoot()
    {
        using var scene = CreateScene();
        var orphan = CreateWithTransform(scene, "orphan", Vector3.Zero);
        orphan.AddComponent(new ParentComponent(99999));

        scene.RebuildHierarchyIndex();

        orphan.GetComponent<ParentComponent>().ParentId.ShouldBeNull();
        scene.GetRootEntities().Select(e => e.Id).ShouldContain(orphan.Id);
    }

    [Fact]
    public void RebuildHierarchyIndex_Cycle_DetachesOneEntityToRoot()
    {
        using var scene = CreateScene();
        var a = CreateWithTransform(scene, "a", Vector3.Zero);
        var b = CreateWithTransform(scene, "b", Vector3.Zero);
        a.AddComponent(new ParentComponent(b.Id));
        b.AddComponent(new ParentComponent(a.Id));

        scene.RebuildHierarchyIndex();

        var aParent = a.GetComponent<ParentComponent>().ParentId;
        var bParent = b.GetComponent<ParentComponent>().ParentId;
        (aParent is null ^ bParent is null).ShouldBeTrue();
        scene.GetRootEntities().Count.ShouldBe(1);
    }

    [Fact]
    public void SetParent_SameParent_PreservesSiblingOrder()
    {
        using var scene = CreateScene();
        var parent = CreateWithTransform(scene, "parent", Vector3.Zero);
        var c1 = CreateWithTransform(scene, "c1", Vector3.Zero);
        var c2 = CreateWithTransform(scene, "c2", Vector3.Zero);
        scene.SetParent(c1, parent);
        scene.SetParent(c2, parent);

        scene.SetParent(c1, parent).ShouldBeTrue();

        scene.GetChildren(parent).Select(e => e.Id).ShouldBe([c1.Id, c2.Id]);
    }

    [Fact]
    public void PrefabV2_Instantiate_BuildsCorrectTree()
    {
        var registry = new ComponentSerializerRegistry();
        var options = new SerializerOptions();
        var prefabSerializer = new PrefabSerializer(registry, options);
        var projectPath = Path.Combine(Path.GetTempPath(), $"prefab-proj-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(projectPath, "assets", "prefabs"));

        try
        {
            using var source = CreateScene();
            var root = CreateWithTransform(source, "weapon", Vector3.Zero);
            var barrel = CreateWithTransform(source, "barrel", new Vector3(0, 1, 0));
            var sight = CreateWithTransform(source, "sight", new Vector3(0, 2, 0));
            source.SetParent(barrel, root);
            source.SetParent(sight, root);
            prefabSerializer.SerializeToPrefab(source, root, "Weapon", projectPath);

            using var dest = CreateScene();
            var prefabPath = Path.Combine(projectPath, "assets", "prefabs", "Weapon.prefab");
            var instance = prefabSerializer.CreateEntityFromPrefab(dest, prefabPath);

            instance.Name.ShouldBe("weapon");
            dest.GetChildren(instance).Count.ShouldBe(2);
            dest.GetChildren(instance).Select(e => e.Name).OrderBy(n => n)
                .ShouldBe(["barrel", "sight"]);
        }
        finally
        {
            if (Directory.Exists(projectPath))
                Directory.Delete(projectPath, recursive: true);
        }
    }

    [Fact]
    public void PrefabV1_StillLoads()
    {
        var registry = new ComponentSerializerRegistry();
        var options = new SerializerOptions();
        var prefabSerializer = new PrefabSerializer(registry, options);
        var projectPath = Path.Combine(Path.GetTempPath(), $"prefab-v1-{Guid.NewGuid():N}");
        var prefabDir = Path.Combine(projectPath, "assets", "prefabs");
        Directory.CreateDirectory(prefabDir);
        var prefabPath = Path.Combine(prefabDir, "OldCube.prefab");

        File.WriteAllText(prefabPath, """
            {
              "Prefab": "OldCube",
              "Version": "1.0",
              "OriginalName": "Cube",
              "Components": [
                {
                  "Name": "TransformComponent",
                  "Translation": [1, 2, 3],
                  "Rotation": [0, 0, 0],
                  "Scale": [1, 1, 1]
                }
              ]
            }
            """);

        try
        {
            using var scene = CreateScene();
            var entity = prefabSerializer.CreateEntityFromPrefab(scene, prefabPath);

            entity.Name.ShouldBe("Cube");
            entity.HasComponent<TransformComponent>().ShouldBeTrue();
            entity.GetComponent<TransformComponent>().Translation.ShouldBe(new Vector3(1, 2, 3));
        }
        finally
        {
            if (Directory.Exists(projectPath))
                Directory.Delete(projectPath, recursive: true);
        }
    }

    [Fact]
    public void ParentComponent_RoundTrip_ThroughRegistry()
    {
        var registry = new ComponentSerializerRegistry();
        var options = new SerializerOptions();
        var entity = Entity.Create(1, "child");
        entity.AddComponent(new ParentComponent(42));

        var array = new System.Text.Json.Nodes.JsonArray();
        registry.SerializeEntity(entity, array, options.Options);

        var loaded = Entity.Create(1, "child");
        foreach (var node in array)
            registry.DeserializeComponent(loaded, node!.AsObject(), options.Options, strict: true);

        loaded.GetComponent<ParentComponent>().ParentId.ShouldBe(42);
    }
}
