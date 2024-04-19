using System.Numerics;
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

    public Vector2 GetPos()
    {
        return SilkNetGameWindow.Mouse.Position;
    }
}