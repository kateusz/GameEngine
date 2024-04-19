using System.Numerics;

namespace Engine.Core.Input;

public interface IMouseState
{
    bool IsMouseButtonPressed(int button);
    Tuple<float, float> GetMousePosition();
    Vector2 GetPos();
}