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

    // todo: not working
    public Vector2 GetPos()
    {
        return SilkNetGameWindow.Mouse.Position;
    }

    // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
    public float X => SilkNetGameWindow.Mouse != null ? SilkNetGameWindow.Mouse.Position.X : 0;
    // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
    public float Y { get; } = SilkNetGameWindow.Mouse != null ? SilkNetGameWindow.Mouse.Position.Y : 0;
}