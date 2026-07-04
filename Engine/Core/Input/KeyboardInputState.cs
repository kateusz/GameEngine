using Engine.Events.Input;
using Input;

namespace Engine.Core.Input;

public sealed class KeyboardInputState : IKeyboardInput
{
    private readonly HashSet<KeyCodes> _held = [];
    private readonly HashSet<KeyCodes> _pressedThisFrame = [];

    public bool IsKeyDown(KeyCodes key) => _held.Contains(key);

    public bool WasKeyPressed(KeyCodes key) => _pressedThisFrame.Contains(key);

    public void Apply(InputEvent inputEvent)
    {
        switch (inputEvent)
        {
            case KeyPressedEvent kpe when kpe.IsRepeat:
                _held.Add(kpe.KeyCode);
                break;
            case KeyPressedEvent kpe:
                _held.Add(kpe.KeyCode);
                _pressedThisFrame.Add(kpe.KeyCode);
                break;
            case KeyReleasedEvent kre:
                _held.Remove(kre.KeyCode);
                break;
        }
    }

    public void EndFrame() => _pressedThisFrame.Clear();
}
