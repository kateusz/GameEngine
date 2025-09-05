using System.Numerics;
using Engine.Renderer;
using Engine.Renderer.Cameras;

namespace Engine.UI;

public class UIRenderer
{
    private readonly IGraphics2D _graphics2D;
    private Vector2 _screenSize;
    
    public UIRenderer(IGraphics2D graphics2D)
    {
        _graphics2D = graphics2D;
        _screenSize = new Vector2(1920, 1080); // Default screen size
    }
    
    public void SetScreenSize(Vector2 screenSize)
    {
        _screenSize = screenSize;
    }
    
    public void BeginUIPass()
    {
        // Use screen coordinates directly (pixels) instead of normalized coordinates
        var left = 0.0f;
        var right = _screenSize.X;
        var top = _screenSize.Y;    // UI coordinates have (0,0) at top-left
        var bottom = 0.0f;          // So flip the Y axis
        
        var uiCamera = new OrthographicCamera(left, right, bottom, top);
        
        _graphics2D.BeginScene(uiCamera);
    }
    
    public void EndUIPass()
    {
        _graphics2D.EndScene();
    }
    
    public void RenderElement(UIElement element)
    {
        if (!element.Visible) return;
        
        // Simply render with original normalized coordinates
        // The UI camera projection handles the coordinate mapping
        element.Render(_graphics2D);
    }
    
    public Vector2 ScreenSpaceToNormalized(Vector2 screenSpaceCoord)
    {
        return new Vector2(
            screenSpaceCoord.X / _screenSize.X,
            screenSpaceCoord.Y / _screenSize.Y
        );
    }
    
    public Vector2 ScreenSize => _screenSize;
}