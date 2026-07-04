using System.Numerics;
using Editor.Features.Viewport;
using Engine.Events.Input;
using Engine.Scene;
using Engine.Scripting;
using ImGuiNET;
using Input;

namespace Editor.Input;

public class EditorInputHandler(
    ISceneContext sceneContext,
    IScriptEngine scriptEngine,
    ShortcutManager shortcutManager,
    IEditorViewport editorViewport)
{
    private readonly HashSet<KeyCodes> _pressedKeys = [];
    private readonly HashSet<int> _pressedMouseButtons = [];

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
            case MouseButtonPressedEvent mbpe:
                _pressedMouseButtons.Add(mbpe.Button);
                break;
            case MouseButtonReleasedEvent mbre:
                _pressedMouseButtons.Remove(mbre.Button);
                break;
        }

        if (sceneContext.State == SceneState.Edit && editorViewport.IsHovered)
        {
            if (windowEvent is MouseScrolledEvent scrollEvent)
                editorViewport.Camera.OnMouseScroll(scrollEvent.YOffset);

            var alt = _pressedKeys.Contains(KeyCodes.LeftAlt) || _pressedKeys.Contains(KeyCodes.RightAlt);

            if (windowEvent is MouseButtonPressedEvent)
                editorViewport.Camera.SetPreviousMousePosition(GetMousePosition());
            else if (windowEvent is MouseMovedEvent moveEvent && alt)
            {
                var currentPos = new Vector2(moveEvent.X, moveEvent.Y);
                var leftDown = _pressedMouseButtons.Contains((int)ImGuiMouseButton.Left);
                var middleDown = _pressedMouseButtons.Contains((int)ImGuiMouseButton.Middle);
                var rightDown = _pressedMouseButtons.Contains((int)ImGuiMouseButton.Right);

                editorViewport.Camera.OnMouseMove(currentPos, pan: middleDown, orbit: leftDown, zoomDrag: rightDown);
            }
        }
        else if (sceneContext.State == SceneState.Play)
        {
            scriptEngine.ProcessEvent(windowEvent);
        }
    }

    private static Vector2 GetMousePosition()
    {
        var pos = ImGui.GetMousePos();
        return new Vector2(pos.X, pos.Y);
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
