using System.Numerics;
using ECS;
using ECS.Systems;
using Editor.Features.History.Commands;
using Editor.Features.Selection;
using Engine.Core.Window;
using Engine.Scene;
using Engine.Scene.Systems;
using NSubstitute;
using SceneComponents;
using Shouldly;

namespace Editor.Tests.History;

public class DestroyEntitySubtreeCommandTests
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

    private static Entity CreateWithTransform(IScene scene, string name, Vector3 translation)
    {
        var entity = scene.CreateEntity(name);
        entity.AddComponent(new TransformComponent(translation, Vector3.Zero, Vector3.One));
        return entity;
    }

    [Fact]
    public void Undo_AfterDestroy_RecreatesSubtreeStructureAndComponents()
    {
        using var scene = CreateScene();
        var root = CreateWithTransform(scene, "root", new Vector3(1, 2, 3));
        var child = CreateWithTransform(scene, "child", new Vector3(4, 5, 6));
        scene.SetParent(child, root);
        var originalRootId = root.Id;
        var originalChildId = child.Id;

        var command = new DestroyEntitySubtreeCommand(scene, originalRootId);
        command.Execute();

        scene.Entities.ShouldBeEmpty();

        command.Undo();

        var restoredRoot = scene.Entities.Single(e => e.Name == "root");
        var restoredChild = scene.Entities.Single(e => e.Name == "child");
        restoredRoot.Id.ShouldNotBe(originalRootId);
        restoredChild.Id.ShouldNotBe(originalChildId);
        scene.GetParent(restoredChild)!.Id.ShouldBe(restoredRoot.Id);
        scene.GetChildren(restoredRoot).Count.ShouldBe(1);

        restoredRoot.GetComponent<TransformComponent>().Translation.ShouldBe(new Vector3(1, 2, 3));
        restoredChild.GetComponent<TransformComponent>().Translation.ShouldBe(new Vector3(4, 5, 6));
    }

    [Fact]
    public void Redo_DestroysRemappedRoot_NotOriginalPreDeleteId()
    {
        using var scene = CreateScene();
        var root = CreateWithTransform(scene, "root", Vector3.Zero);
        var child = CreateWithTransform(scene, "child", Vector3.UnitX);
        scene.SetParent(child, root);
        var originalRootId = root.Id;

        var command = new DestroyEntitySubtreeCommand(scene, originalRootId);
        command.Execute();
        command.Undo();

        var remappedRootId = scene.Entities.Single(e => e.Name == "root").Id;
        remappedRootId.ShouldNotBe(originalRootId);

        // Redo = Execute again; must destroy remapped root (W3), not original pre-delete id
        command.Execute();

        scene.Context.Contains(remappedRootId).ShouldBeFalse();
        scene.Entities.ShouldBeEmpty();
    }

    [Fact]
    public void Undo_RestoresParentAndChildren_Cascade()
    {
        using var scene = CreateScene();
        var root = CreateWithTransform(scene, "root", Vector3.Zero);
        var mid = CreateWithTransform(scene, "mid", Vector3.Zero);
        var leaf1 = CreateWithTransform(scene, "leaf1", Vector3.Zero);
        var leaf2 = CreateWithTransform(scene, "leaf2", Vector3.Zero);
        scene.SetParent(mid, root);
        scene.SetParent(leaf1, mid);
        scene.SetParent(leaf2, mid);

        var command = new DestroyEntitySubtreeCommand(scene, root.Id);
        command.Execute();
        scene.Entities.ShouldBeEmpty();

        command.Undo();

        var restoredRoot = scene.Entities.Single(e => e.Name == "root");
        var restoredMid = scene.Entities.Single(e => e.Name == "mid");
        var restoredLeaf1 = scene.Entities.Single(e => e.Name == "leaf1");
        var restoredLeaf2 = scene.Entities.Single(e => e.Name == "leaf2");

        scene.GetParent(restoredMid)!.Id.ShouldBe(restoredRoot.Id);
        scene.GetParent(restoredLeaf1)!.Id.ShouldBe(restoredMid.Id);
        scene.GetParent(restoredLeaf2)!.Id.ShouldBe(restoredMid.Id);
        scene.GetChildren(restoredMid).Count.ShouldBe(2);
        scene.GetRootEntities().Select(e => e.Name).ShouldBe(["root"]);
    }

    [Fact]
    public void Command_DoesNotMutateSelection()
    {
        using var scene = CreateScene();
        var root = CreateWithTransform(scene, "root", Vector3.Zero);
        var selection = Substitute.For<IEditorSelection>();
        selection.SelectedEntity.Returns(root);

        var command = new DestroyEntitySubtreeCommand(scene, root.Id);
        command.Execute();
        command.Undo();
        command.Execute();

        selection.DidNotReceiveWithAnyArgs().Select(default!, default);
    }

    [Fact]
    public void Undo_PreservesNativeScriptTypeName_ViaCloneBag()
    {
        using var scene = CreateScene();
        var root = CreateWithTransform(scene, "scripted", Vector3.Zero);
        root.AddComponent(new NativeScriptComponent { ScriptTypeName = "Games.Demo.PlayerScript" });

        var command = new DestroyEntitySubtreeCommand(scene, root.Id);
        command.Execute();
        command.Undo();

        var restored = scene.Entities.Single(e => e.Name == "scripted");
        restored.GetComponent<NativeScriptComponent>().ScriptTypeName.ShouldBe("Games.Demo.PlayerScript");
    }

    [Fact]
    public void Undo_KeepsSubtreeUnderExternalParent()
    {
        using var scene = CreateScene();
        var external = CreateWithTransform(scene, "external", Vector3.Zero);
        var root = CreateWithTransform(scene, "root", Vector3.Zero);
        var child = CreateWithTransform(scene, "child", Vector3.UnitX);
        scene.SetParent(root, external);
        scene.SetParent(child, root);

        var command = new DestroyEntitySubtreeCommand(scene, root.Id);
        command.Execute();
        scene.GetChildren(external).ShouldBeEmpty();

        command.Undo();

        var restoredRoot = scene.Entities.Single(e => e.Name == "root");
        scene.GetParent(restoredRoot)!.Id.ShouldBe(external.Id);
        scene.GetChildren(external).Select(e => e.Name).ShouldBe(["root"]);
    }
}
