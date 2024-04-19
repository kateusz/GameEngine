using System.Numerics;
using Engine.Core.Input;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine.Platform.OpenTK.Input;

public class OpenTKMouseState : IMouseState
{
    public bool IsMouseButtonPressed(int button)
    {
        return OpenTKGameWindow.Mouse.IsButtonDown((MouseButton)button);
    }

    public Tuple<float, float> GetMousePosition()
    {
        return new Tuple<float, float>(OpenTKGameWindow.Mouse.X, OpenTKGameWindow.Mouse.Y);
    }

    public Vector2 GetPos()
    {
        var pos = OpenTKGameWindow.Mouse.Position;
        return new Vector2(pos.X, pos.Y);
    }
    
    public float GetMouseY()
    {
        return OpenTKGameWindow.Mouse.Y;
    }
}