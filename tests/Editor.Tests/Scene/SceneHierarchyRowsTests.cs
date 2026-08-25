using ECS;
using ECS.Systems;
using Editor.Features.Scene;
using Engine.Core.Window;
using Engine.Scene;
using Engine.Scene.Systems;
using NSubstitute;
using SceneComponents;
using Shouldly;

namespace Editor.Tests.Scene;

public class SceneHierarchyRowsTests
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

    private static Entity Create(IScene scene, string name)
    {
        var entity = scene.CreateEntity(name);
        entity.AddComponent(new TransformComponent());
        return entity;
    }

    [Fact]
    public void CollectVisibleRows_OnlyWalksExpandedBranches()
    {
        using var scene = CreateScene();
        var parent = Create(scene, "parent");
        var child = Create(scene, "child");
        var grandchild = Create(scene, "grandchild");
        scene.SetParent(child, parent);
        scene.SetParent(grandchild, child);
        var other = Create(scene, "other");
        scene.SetParent(other, parent);

        var rows = new List<HierarchyRow>();
        foreach (var root in scene.GetRootEntities())
            SceneHierarchyPanel.CollectVisibleRows(scene, root, 0, expandedIds: [], filterVisibleIds: null, rows);

        rows.Select(r => r.Entity.Name).ShouldBe(["parent"]);
        rows[0].HasChildren.ShouldBeTrue();

        rows.Clear();
        foreach (var root in scene.GetRootEntities())
            SceneHierarchyPanel.CollectVisibleRows(scene, root, 0, expandedIds: [parent.Id], filterVisibleIds: null, rows);

        rows.Select(r => r.Entity.Name).ShouldBe(["parent", "child", "other"]);
        rows[1].Depth.ShouldBe(1);
        rows[1].HasChildren.ShouldBeTrue();
        rows[2].HasChildren.ShouldBeFalse();

        rows.Clear();
        foreach (var root in scene.GetRootEntities())
            SceneHierarchyPanel.CollectVisibleRows(
                scene, root, 0, expandedIds: [parent.Id, child.Id], filterVisibleIds: null, rows);

        rows.Select(r => r.Entity.Name).ShouldBe(["parent", "child", "grandchild", "other"]);
        rows[2].Depth.ShouldBe(2);
    }

    [Fact]
    public void CollectVisibleRows_Filter_SkipsHiddenSiblings()
    {
        using var scene = CreateScene();
        var parent = Create(scene, "parent");
        var miss = Create(scene, "miss");
        var hit = Create(scene, "hit");
        scene.SetParent(miss, parent);
        scene.SetParent(hit, parent);

        var rows = new List<HierarchyRow>();
        foreach (var root in scene.GetRootEntities())
            SceneHierarchyPanel.CollectVisibleRows(
                scene, root, 0, expandedIds: [parent.Id], filterVisibleIds: [parent.Id, hit.Id], rows);

        rows.Select(r => r.Entity.Name).ShouldBe(["parent", "hit"]);
    }
}
