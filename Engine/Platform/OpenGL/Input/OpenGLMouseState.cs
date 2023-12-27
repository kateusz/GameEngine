using Engine.Core.Input;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine.Platform.OpenGL.Input;

public class OpenGLMouseState : IMouseState
{
    public bool IsMouseButtonPressed(int button)
    {
        return OpenGLWindow.Mouse.IsButtonDown((MouseButton)button);
    }

    public Tuple<float, float> GetMousePosition()
    {
        return new Tuple<float, float>(OpenGLWindow.Mouse.X, OpenGLWindow.Mouse.Y);
    }

    public float GetMouseX()
    {
        return OpenGLWindow.Mouse.X;
    }

    public float GetMouseY()
    {
        return OpenGLWindow.Mouse.Y;
    }
}