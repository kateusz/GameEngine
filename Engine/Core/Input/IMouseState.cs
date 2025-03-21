using System.Numerics;

namespace Engine.Core.Input;

public interface IMouseState
{
    bool IsMouseButtonPressed(int button);
    Vector2 GetPos();
    float ScrollY { get; }
    float X { get; }
    float Y { get; }
}