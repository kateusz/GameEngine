using Editor.Features.Viewport;
using Editor.Input;
using Engine.Events.Input;
using Engine.Scene;
using Input;
using NSubstitute;
using Shouldly;

namespace Editor.Tests.Input;

public class EditorInputHandlerTests
{
    private static EditorInputHandler Create(ISceneContext sceneContext, ShortcutManager shortcuts, IEditorViewport viewport) =>
        new(sceneContext, shortcuts, viewport);

    private static ISceneContext Context(SceneState state)
    {
        var sceneContext = Substitute.For<ISceneContext>();
        sceneContext.State.Returns(state);
        return sceneContext;
    }

    [Fact]
    public void Handle_Edit_ForwardsToViewport()
    {
        var viewport = Substitute.For<IEditorViewport>();
        var handler = Create(Context(SceneState.Edit), new ShortcutManager(), viewport);
        var ev = new KeyPressedEvent(KeyCodes.W, false);

        handler.Handle(ev);

        viewport.Received(1).HandleWindowInput(ev);
    }

    [Fact]
    public void Handle_Play_DoesNotForwardToViewport()
    {
        var viewport = Substitute.For<IEditorViewport>();
        var handler = Create(Context(SceneState.Play), new ShortcutManager(), viewport);

        handler.Handle(new KeyPressedEvent(KeyCodes.W, false));

        viewport.DidNotReceive().HandleWindowInput(Arg.Any<InputEvent>());
    }

    [Fact]
    public void Handle_CtrlS_MarksEventHandled()
    {
        var shortcuts = new ShortcutManager();
        shortcuts.RegisterShortcut(new KeyboardShortcut(
            KeyCodes.S, KeyModifiers.CtrlOnly, () => { }, "Save", "File"));
        var handler = Create(Context(SceneState.Edit), shortcuts, Substitute.For<IEditorViewport>());

        handler.Handle(new KeyPressedEvent(KeyCodes.LeftControl, false));
        var save = new KeyPressedEvent(KeyCodes.S, false);
        handler.Handle(save);

        save.IsHandled.ShouldBeTrue();
    }
}
