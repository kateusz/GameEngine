using System.Numerics;

namespace Input;

public interface IMouseInput
{
    Vector2 Position { get; }

    Vector2 Delta { get; }

    Vector2 Scroll { get; }

    bool IsButtonDown(int button);

    bool WasButtonPressed(int button);
}
