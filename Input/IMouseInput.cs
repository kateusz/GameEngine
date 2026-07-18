using System.Numerics;

namespace Input;

public interface IMouseInput
{
    Vector2 Position { get; }

    bool IsButtonDown(int button);

    bool WasButtonPressed(int button);

    void EndFrame();
}
