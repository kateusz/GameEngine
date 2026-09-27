using ECS;
using ECS.Systems;
using Editor.Commands;
using Editor.Features.Scene;
using Editor.Features.Selection;
using Editor.Features.Settings;
using Editor.Input;
using Engine.Core.Window;
using Engine.Scene;
using Engine.Scene.Systems;
using Input;
using NSubstitute;
using SceneComponents;
using Scripting;
using Shouldly;

namespace Editor.Tests.Commands;

public class CommandRegistryTests
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

    private static (CommandRegistry Registry, IEditorSelection Selection, SceneHierarchyPanel Hierarchy) Create(
        ISceneContext? sceneContext = null)
    {
        var selection = Substitute.For<IEditorSelection>();
        var hierarchy = new SceneHierarchyPanel(
            null!,
            null!,
            selection,
            null!,
            Substitute.For<IEditorPreferences>(),
            Substitute.For<ISceneManager>());
        var context = sceneContext ?? Substitute.For<ISceneContext>();
        return (new CommandRegistry(context, selection, hierarchy), selection, hierarchy);
    }

    [Fact]
    public void Register_And_Execute_ById()
    {
        var (registry, _, _) = Create();
        var ran = false;
        registry.Register(new EditorCommand("t.run", "Run", "Test", () => ran = true)).ShouldBeTrue();

        registry.Execute("t.run");

        ran.ShouldBeTrue();
    }

    [Fact]
    public void Register_DuplicateId_Rejected()
    {
        var (registry, _, _) = Create();
        registry.Register(new EditorCommand("t.dup", "A", "Test", () => { })).ShouldBeTrue();
        registry.Register(new EditorCommand("t.dup", "B", "Test", () => { })).ShouldBeFalse();
    }

    [Fact]
    public void Execute_WhenCanExecuteFalse_DoesNotRun()
    {
        var (registry, _, _) = Create();
        var ran = false;
        registry.Register(new EditorCommand(
            "t.blocked", "Blocked", "Test",
            () => ran = true,
            () => CanExecuteResult.No("nope")));

        registry.Execute("t.blocked");

        ran.ShouldBeFalse();
    }

    [Fact]
    public void Execute_UnknownId_DoesNotThrow()
    {
        var (registry, _, _) = Create();
        Should.NotThrow(() => registry.Execute("missing.id"));
    }

    [Fact]
    public void GetWorkingSet_NoActiveScene_HasNoEntityRows()
    {
        var sceneContext = Substitute.For<ISceneContext>();
        sceneContext.ActiveScene.Returns((IScene?)null);
        var (registry, _, _) = Create(sceneContext);
        registry.Register(new EditorCommand("t.one", "One", "Test", () => { }));

        var set = registry.GetWorkingSet();
        set.ShouldNotContain(i => i.Id.StartsWith(EditorCommandIds.EntityGotoPrefix));
        set.ShouldContain(i => i.Id == "t.one");
    }

    [Fact]
    public void EntityJump_SelectsEntity()
    {
        using var scene = CreateScene();
        var entity = scene.CreateEntity("Hero");
        entity.AddComponent(new TransformComponent());

        var sceneContext = Substitute.For<ISceneContext>();
        sceneContext.ActiveScene.Returns(scene);
        var (registry, selection, hierarchy) = Create(sceneContext);
        hierarchy.SetScene(scene);

        registry.GetWorkingSet().ShouldContain(i => i.Id == EditorCommandIds.EntityGoto(entity.Id) && i.Title == "Hero");

        registry.Execute(EditorCommandIds.EntityGoto(entity.Id));

        selection.Received(1).Select(entity, SelectionSource.Code);
    }

    [Fact]
    public void Shortcut_ResolvesToRegistryExecute()
    {
        var (registry, _, _) = Create();
        var ran = false;
        registry.Register(new EditorCommand("t.save", "Save", "File", () => ran = true));

        var shortcuts = new ShortcutManager();
        shortcuts.RegisterShortcut(new KeyboardShortcut(
            KeyCodes.S, KeyModifiers.CtrlOnly, () => registry.Execute("t.save"), "Save", "File"));

        shortcuts.HandleKeyPress(KeyCodes.S, control: true, shift: false, alt: false).ShouldBeTrue();
        ran.ShouldBeTrue();
    }
}
