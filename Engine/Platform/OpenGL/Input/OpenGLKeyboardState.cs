using Engine.Core.Input;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine.Platform.OpenGL.Input;

public class OpenGLKeyboardState : IKeyboardState
{
    public bool IsKeyPressed(KeyCodes keycode)
    {
        return OpenGLWindow.Keyboard.IsKeyDown((Keys)((int)keycode));
    }
}