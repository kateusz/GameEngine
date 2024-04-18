using Engine.Core.Input;
using Silk.NET.Input;

namespace Engine.Platform.SilkNet.Input;

public class SIlkNetMouseState : IMouseState
{
    public bool IsMouseButtonPressed(int button)
    {
        return SilkNetGameWindow.Mouse.IsButtonPressed((MouseButton)button);
    }

    public Tuple<float, float> GetMousePosition()
    {
        return new Tuple<float, float>(SilkNetGameWindow.Mouse.Position.X, SilkNetGameWindow.Mouse.Position.Y);
    }

    public float GetMouseX()
    {
        return SilkNetGameWindow.Mouse.Position.X;
    }

    public float GetMouseY()
    {
        return SilkNetGameWindow.Mouse.Position.Y;
    }
}