using Engine.Core.Input;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine.Platform.OpenTK.Input;

public class OpenTKKeyboardState : IKeyboardState
{
    public bool IsKeyPressed(KeyCodes keycode)
    {
        return OpenTKGameWindow.Keyboard.IsKeyDown((Keys)((int)keycode));
    }
}