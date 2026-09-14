using System.Numerics;
using ECS;
using Editor.Features.Viewport;
using Editor.Input;
using Engine.Core.Window;
using Engine.Events;
using Engine.Events.Input;
using Engine.Scene;
using Engine.Scripting;
using Input;
using NSubstitute;
using Scripting;

namespace Editor.Tests.Input;

public class EditorInputHandlerTests
{
    private static EditorInputHandler Create(
        ISceneContext sceneContext,
        IScriptEngine scriptEngine,
        ShortcutManager shortcuts,
        IPointerSurface? surface = null,
        IMouseInput? mouse = null)
    {
        return new EditorInputHandler(
            sceneContext,
            scriptEngine,
            mouse ?? Substitute.For<IMouseInput>(),
            surface ?? Substitute.For<IPointerSurface>(),
            shortcuts,
            Substitute.For<IEditorViewport>());
    }

    private static ISceneContext PlayContext(IScene scene, ScriptRuntimeStore store)
    {
        var sceneContext = Substitute.For<ISceneContext>();
        sceneContext.State.Returns(SceneState.Play);
        sceneContext.ActiveScene.Returns(scene);
        sceneContext.ActiveScriptRuntimeStore.Returns(store);
        return sceneContext;
    }

    [Fact]
    public void Handle_Play_ForwardsUnconsumedKeyToScripts()
    {
        var scene = Substitute.For<IScene>();
        var context = Substitute.For<IContext>();
        scene.Context.Returns(context);
        var store = new ScriptRuntimeStore();
        var scriptEngine = Substitute.For<IScriptEngine>();
        var handler = Create(PlayContext(scene, store), scriptEngine, new ShortcutManager());

        handler.Handle(new KeyPressedEvent(KeyCodes.W, false));

        scriptEngine.Received(1).ProcessEvent(
            Arg.Any<Event>(), context, store);
    }

    [Fact]
    public void Handle_Play_ShortcutHandled_DoesNotProcessScriptEvent()
    {
        var scene = Substitute.For<IScene>();
        scene.Context.Returns(Substitute.For<IContext>());
        var store = new ScriptRuntimeStore();
        var scriptEngine = Substitute.For<IScriptEngine>();
        var shortcuts = new ShortcutManager();
        shortcuts.RegisterShortcut(new KeyboardShortcut(
            KeyCodes.S, KeyModifiers.CtrlOnly, () => { }, "Save", "File"));
        var handler = Create(PlayContext(scene, store), scriptEngine, shortcuts);

        handler.Handle(new KeyPressedEvent(KeyCodes.LeftControl, false));
        handler.Handle(new KeyPressedEvent(KeyCodes.S, false));

        scriptEngine.DidNotReceive().ProcessEvent(
            Arg.Is<Event>(e => IsKeyS(e)),
            Arg.Any<IContext>(),
            Arg.Any<ScriptRuntimeStore>());
    }

    private static bool IsKeyS(Event e) =>
        e is KeyPressedEvent pressed && pressed.KeyCode == KeyCodes.S;

    [Fact]
    public void Handle_Play_MouseOutsideSurface_DoesNotProcessScriptEvent()
    {
        var scene = Substitute.For<IScene>();
        scene.Context.Returns(Substitute.For<IContext>());
        var store = new ScriptRuntimeStore();
        var scriptEngine = Substitute.For<IScriptEngine>();
        var surface = new PointerSurface();
        surface.Set(new Vector2(100f, 100f), new Vector2(200f, 200f));
        var mouse = Substitute.For<IMouseInput>();
        mouse.Position.Returns(new Vector2(10f, 10f));
        var handler = Create(PlayContext(scene, store), scriptEngine, new ShortcutManager(), surface, mouse);

        handler.Handle(new MouseButtonPressedEvent(MouseButtons.Left));

        scriptEngine.DidNotReceive().ProcessEvent(
            Arg.Any<Event>(), Arg.Any<IContext>(), Arg.Any<ScriptRuntimeStore>());
    }
}
