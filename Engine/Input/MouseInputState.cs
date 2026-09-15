using System.Numerics;
using Engine.Events.Input;
using Input;

namespace Engine.Input;

public sealed class MouseInputState : IMouseInput
{
    private readonly HashSet<int> _held = [];
    private readonly HashSet<int> _pressedThisFrame = [];
    private bool _hasPosition;

    public Vector2 Position { get; private set; }
    public Vector2 Delta { get; private set; }
    public Vector2 Scroll { get; private set; }

    public bool IsButtonDown(int button) => _held.Contains(button);

    public bool WasButtonPressed(int button) => _pressedThisFrame.Contains(button);

    public void Apply(InputEvent inputEvent)
    {
        switch (inputEvent)
        {
            case MouseMovedEvent moved:
            {
                var next = new Vector2(moved.X, moved.Y);
                Delta = _hasPosition ? next - Position : Vector2.Zero;
                Position = next;
                _hasPosition = true;
                break;
            }
            case MouseScrolledEvent scrolled:
                Scroll += new Vector2(scrolled.XOffSet, scrolled.YOffset);
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

    public void EndFrame()
    {
        _pressedThisFrame.Clear();
        Delta = Vector2.Zero;
        Scroll = Vector2.Zero;
    }
}
