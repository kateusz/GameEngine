namespace Input;

public interface IKeyboardInput
{
    bool IsKeyDown(KeyCodes key);

    bool WasKeyPressed(KeyCodes key);

    void EndFrame();
}
