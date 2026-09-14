using Editor.Features.Viewport;
using Engine.Events.Input;
using Engine.Scene;
using ImGuiNET;
using Input;

namespace Editor.Input;

public class EditorInputHandler(
    ISceneContext sceneContext,
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
    }

    private void OnKeyPressed(KeyPressedEvent keyPressedEvent)
    {
        if (keyPressedEvent.IsRepeat)
            return;

        if (ImGui.GetCurrentContext() != IntPtr.Zero)
        {
            var io = ImGui.GetIO();
            if (io.WantCaptureKeyboard)
                return;
        }

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
