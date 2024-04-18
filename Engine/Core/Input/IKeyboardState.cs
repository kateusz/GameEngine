namespace Engine.Core.Input;

public interface IKeyboardState
{
    bool IsKeyPressed(KeyCodes keycode);
}