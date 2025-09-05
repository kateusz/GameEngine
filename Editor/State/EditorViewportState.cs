using System.Numerics;

namespace Editor.State;

public class EditorViewportState
{
    public Vector2 ViewportSize { get; set; }
    public Vector2[] ViewportBounds { get; } = new Vector2[2];
    public bool ViewportHovered { get; set; }
    public bool ViewportFocused { get; set; }
    
    public Vector2 MousePosition { get; set; }
    public Vector2 RelativeMousePosition { get; set; }
    
    public bool IsMouseInViewport => 
        RelativeMousePosition.X >= 0 && RelativeMousePosition.Y >= 0 && 
        RelativeMousePosition.X < ViewportSize.X && RelativeMousePosition.Y < ViewportSize.Y;
        
    public void UpdateMousePosition(Vector2 mousePos)
    {
        MousePosition = mousePos;
        RelativeMousePosition = new Vector2(
            mousePos.X - ViewportBounds[0].X,
            ViewportBounds[1].Y - ViewportBounds[0].Y - (mousePos.Y - ViewportBounds[0].Y)
        );
    }
}