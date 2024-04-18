namespace Engine.Core.Input;

public interface IMouseState
{
    bool IsMouseButtonPressed(int button);
    Tuple<float, float> GetMousePosition();
    float GetMouseX();
    float GetMouseY();
}