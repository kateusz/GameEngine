using Editor.Features.Viewport;
using Engine.Core.Input;
using Engine.Events.Input;
using Engine.Scene;
using Engine.Scripting;
using Engine.UI.Paper;
using ImGuiNET;
using Input;

namespace Editor.Input;

public class EditorInputHandler(
    ISceneContext sceneContext,
    IScriptEngine scriptEngine,
    IKeyboardInput keyboardInput,
    IMouseInput mouseInput,
    PaperInputAdapter paperInputAdapter,
    PaperInputGate paperInputGate,
    ShortcutManager shortcutManager,
    IEditorViewport editorViewport)
{
    private readonly HashSet<KeyCodes> _pressedKeys = [];

    public void Handle(InputEvent windowEvent)
    {
        switch (windowEvent)
        {
            case KeyPressedEvent kpe:
                _pressedKeys.Add(kpe.KeyCode);
                OnKeyPressed(kpe);
                break;
            case KeyReleasedEvent kre:
                _pressedKeys.Remove(kre.KeyCode);
                break;
        }

        if (sceneContext.State == SceneState.Edit)
            editorViewport.HandleWindowInput(windowEvent);
        else if (sceneContext.State == SceneState.Play)
        {
            paperInputAdapter.Apply(windowEvent);

            if (keyboardInput is KeyboardInputState keyboardState)
                keyboardState.Apply(windowEvent);

            if (mouseInput is MouseInputState mouseState)
                mouseState.Apply(windowEvent);

            if (PaperInputGate.Blocks(windowEvent, paperInputGate))
            {
                windowEvent.IsHandled = true;
                return;
            }

            if (sceneContext is { ActiveScene: { } scene, ActiveScriptRuntimeStore: { } store })
                scriptEngine.ProcessEvent(windowEvent, scene.Context, store);
        }
    }

    private void OnKeyPressed(KeyPressedEvent keyPressedEvent)
    {
        if (keyPressedEvent.IsRepeat)
            return;

        var io = ImGui.GetIO();
        if (io.WantCaptureKeyboard)
            return;

        var control = _pressedKeys.Contains(KeyCodes.LeftControl) ||
                      _pressedKeys.Contains(KeyCodes.RightControl);
        var shift = _pressedKeys.Contains(KeyCodes.LeftShift) ||
                    _pressedKeys.Contains(KeyCodes.RightShift);
        var alt = _pressedKeys.Contains(KeyCodes.LeftAlt) ||
                  _pressedKeys.Contains(KeyCodes.RightAlt);

        if (shortcutManager.HandleKeyPress(keyPressedEvent.KeyCode, control, shift, alt))
            keyPressedEvent.IsHandled = true;
    }
}
