using Engine.Core.Input;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine.Platform.OpenGL.Input;

public class OpenGLMouseState : IMouseState
{
    public bool IsMouseButtonPressed(int button)
    {
        return OpenGLGameWindow.Mouse.IsButtonDown((MouseButton)button);
    }

    public Tuple<float, float> GetMousePosition()
    {
        return new Tuple<float, float>(OpenGLGameWindow.Mouse.X, OpenGLGameWindow.Mouse.Y);
    }

    public float GetMouseX()
    {
        return OpenGLGameWindow.Mouse.X;
    }

    public float GetMouseY()
    {
        return OpenGLGameWindow.Mouse.Y;
    }
}