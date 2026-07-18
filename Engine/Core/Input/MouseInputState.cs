using System.Numerics;
using Engine.Events.Input;
using Input;

namespace Engine.Core.Input;

public sealed class MouseInputState : IMouseInput
{
    private readonly HashSet<int> _held = [];
    private readonly HashSet<int> _pressedThisFrame = [];

    public Vector2 Position { get; private set; }

    public bool IsButtonDown(int button) => _held.Contains(button);

    public bool WasButtonPressed(int button) => _pressedThisFrame.Contains(button);

    public void Apply(InputEvent inputEvent)
    {
        switch (inputEvent)
        {
            case MouseMovedEvent moved:
                Position = new Vector2(moved.X, moved.Y);
                break;
            case MouseButtonPressedEvent pressed:
                _held.Add(pressed.Button);
                _pressedThisFrame.Add(pressed.Button);
                break;
            case MouseButtonReleasedEvent released:
                _held.Remove(released.Button);
                break;
        }
    }

    public void EndFrame() => _pressedThisFrame.Clear();
}
