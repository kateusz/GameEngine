using System.Numerics;
using ECS;
using Editor.Features.History;
using Editor.Features.History.Commands;
using Engine.Scene;
using NSubstitute;
using SceneComponents;
using SceneComponents.Camera;
using Shouldly;

namespace Editor.Tests.History;

/// <summary>
/// Wrap-site coverage checklist (command invert covered here; sites wire via history.Execute):
/// Tools ×3: MoveTool / RotateTool / ScaleTool → SetTransformCommand (see SetTransformCommandTests)
/// Hierarchy ×1: SceneHierarchyPanel Delete → DestroyEntitySubtreeCommand (see DestroyEntitySubtreeCommandTests)
/// Components ×4:
/// 1. ComponentSelector — AddComponentCommand (incl. Transform auto-add compound)
/// 2. ComponentEditorRegistry — RemoveComponentCommand on "-" remove
/// 3. ScriptComponentEditor — NativeScript add/remove via history (NativeScript_AddRemove_ViaHistory)
/// 4. GameComponentFactory — AddComponentDynamic attach via history (.cs file create not undone)
/// </summary>
public class ComponentCommandTests
{
    private readonly ISceneContext _sceneContext = Substitute.For<ISceneContext>();

    public ComponentCommandTests()
    {
        _sceneContext.State.Returns(SceneState.Edit);
    }

    private EditorHistory CreateHistory() => new(_sceneContext);

    [Fact]
    public void Add_ThenUndo_Removes_Redo_ReAdds()
    {
        var entity = Entity.Create(1, "e");
        var history = CreateHistory();
        var command = new AddComponentCommand(entity, new TransformComponent());

        history.Execute(command);
        entity.HasComponent<TransformComponent>().ShouldBeTrue();
        history.CanUndo.ShouldBeTrue();

        history.Undo();
        entity.HasComponent<TransformComponent>().ShouldBeFalse();
        history.CanRedo.ShouldBeTrue();

        history.Redo();
        entity.HasComponent<TransformComponent>().ShouldBeTrue();
    }

    [Fact]
    public void Remove_StoresClone_Undo_Restores_Redo_RemovesAgain()
    {
        var entity = Entity.Create(2, "e");
        var original = new TransformComponent(new Vector3(1, 2, 3), Vector3.Zero, Vector3.One);
        entity.AddComponent(original);
        var history = CreateHistory();
        var command = new RemoveComponentCommand(entity, typeof(TransformComponent));

        history.Execute(command);
        entity.HasComponent<TransformComponent>().ShouldBeFalse();

        history.Undo();
        entity.HasComponent<TransformComponent>().ShouldBeTrue();
        entity.GetComponent<TransformComponent>().Translation.ShouldBe(new Vector3(1, 2, 3));

        history.Redo();
        entity.HasComponent<TransformComponent>().ShouldBeFalse();
    }

    [Fact]
    public void W2_CompoundAdd_IsOneStackEntry_UndoRemovesBoth_RedoRestoresBoth()
    {
        var entity = Entity.Create(3, "camera-entity");
        entity.HasComponent<TransformComponent>().ShouldBeFalse();

        var history = CreateHistory();
        var camera = new CameraComponent { Primary = true, AspectRatio = 16f / 9f };
        // W2: Transform auto-add + primary = one Execute / one undo step
        history.Execute(new AddComponentCommand(entity, camera, autoAddTransform: true));

        entity.HasComponent<TransformComponent>().ShouldBeTrue();
        entity.HasComponent<CameraComponent>().ShouldBeTrue();
        entity.GetComponent<CameraComponent>().Primary.ShouldBeTrue();

        // One stack entry only
        history.Undo();
        history.CanUndo.ShouldBeFalse();

        entity.HasComponent<CameraComponent>().ShouldBeFalse();
        entity.HasComponent<TransformComponent>().ShouldBeFalse();

        history.Redo();
        entity.HasComponent<TransformComponent>().ShouldBeTrue();
        entity.HasComponent<CameraComponent>().ShouldBeTrue();
        entity.GetComponent<CameraComponent>().Primary.ShouldBeTrue();
    }

    [Fact]
    public void W2_CompoundAdd_WhenTransformAlreadyPresent_UndoDoesNotRemoveTransform()
    {
        var entity = Entity.Create(4, "e");
        entity.AddComponent(new TransformComponent());
        var history = CreateHistory();

        history.Execute(new AddComponentCommand(
            entity, new CameraComponent { Primary = true }, autoAddTransform: true));

        entity.HasComponent<CameraComponent>().ShouldBeTrue();
        entity.HasComponent<TransformComponent>().ShouldBeTrue();

        history.Undo();

        entity.HasComponent<CameraComponent>().ShouldBeFalse();
        entity.HasComponent<TransformComponent>().ShouldBeTrue();
    }

    [Fact]
    public void Add_WhenComponentAlreadyPresent_DoesNotPush()
    {
        var entity = Entity.Create(7, "e");
        entity.AddComponent(new TransformComponent());
        var history = CreateHistory();

        history.Execute(new AddComponentCommand(entity, new TransformComponent()));

        history.CanUndo.ShouldBeFalse();
        entity.HasComponent<TransformComponent>().ShouldBeTrue();
    }

    [Fact]
    public void Remove_ThenUndo_RestoresClonedValues_NotSameInstance()
    {
        var entity = Entity.Create(5, "e");
        var original = new CameraComponent { Primary = true, AspectRatio = 1.5f };
        entity.AddComponent(original);

        var command = new RemoveComponentCommand(entity, typeof(CameraComponent));
        command.Execute().ShouldBeTrue();
        command.Undo();

        var restored = entity.GetComponent<CameraComponent>();
        restored.ShouldNotBeSameAs(original);
        restored.Primary.ShouldBeTrue();
        restored.AspectRatio.ShouldBe(1.5f);
    }

    [Fact]
    public void NativeScript_AddRemove_ViaHistory_UndoRedo()
    {
        // ScriptComponentEditor wrap site: NativeScript add/remove through history.Execute
        var entity = Entity.Create(6, "scripted");
        var history = CreateHistory();

        history.Execute(new AddComponentCommand(
            entity, new NativeScriptComponent { ScriptTypeName = "Games.Demo.PlayerScript" }));
        entity.HasComponent<NativeScriptComponent>().ShouldBeTrue();
        entity.GetComponent<NativeScriptComponent>().ScriptTypeName.ShouldBe("Games.Demo.PlayerScript");

        history.Undo();
        entity.HasComponent<NativeScriptComponent>().ShouldBeFalse();

        history.Redo();
        entity.HasComponent<NativeScriptComponent>().ShouldBeTrue();
        entity.GetComponent<NativeScriptComponent>().ScriptTypeName.ShouldBe("Games.Demo.PlayerScript");

        history.Execute(new RemoveComponentCommand(entity, typeof(NativeScriptComponent)));
        entity.HasComponent<NativeScriptComponent>().ShouldBeFalse();

        history.Undo();
        entity.HasComponent<NativeScriptComponent>().ShouldBeTrue();
        entity.GetComponent<NativeScriptComponent>().ScriptTypeName.ShouldBe("Games.Demo.PlayerScript");
    }
}
