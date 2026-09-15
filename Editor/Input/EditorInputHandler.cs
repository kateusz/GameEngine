using System.Numerics;
using Editor.Features.Viewport;
using Engine.Core.Window;
using Engine.Events.Input;
using Engine.Scene;
using Engine.Scripting;
using ImGuiNET;
using Input;

namespace Editor.Input;

public class EditorInputHandler(
    ISceneContext sceneContext,
    IScriptEngine scriptEngine,
    IMouseInput mouseInput,
    IPointerSurface pointerSurface,
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
        {
            editorViewport.HandleWindowInput(windowEvent);
            return;
        }

        if (sceneContext.State != SceneState.Play)
            return;

        if (!ShouldDispatchToScripts(windowEvent))
            return;

        if (sceneContext is { ActiveScene: { } scene, ActiveScriptRuntimeStore: { } store })
            scriptEngine.ProcessEvent(windowEvent, scene.Context, store);
    }

    private bool ShouldDispatchToScripts(InputEvent windowEvent)
    {
        if (windowEvent.IsHandled)
            return false;

        if (windowEvent is not MouseEvent)
            return true;

        var pos = windowEvent is MouseMovedEvent moved
            ? new Vector2(moved.X, moved.Y)
            : mouseInput.Position;
        return pointerSurface.Contains(pos);
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
