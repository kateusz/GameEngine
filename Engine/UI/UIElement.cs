using System.Numerics;
using Engine.Renderer;

namespace Engine.UI;

public abstract class UIElement
{
    public Vector2 Position { get; set; } = Vector2.Zero;
    public Vector2 Size { get; set; } = new(100, 30);
    public UIStyle Style { get; set; } = UIStyle.Default;
    public bool Visible { get; set; } = true;
    public bool Interactive { get; set; } = true;
    public int ZOrder { get; set; } = 0;
    public string Id { get; set; } = string.Empty;
    
    protected bool _isDirty = true;
    
    public Rectangle Bounds => new(Position, Size);
    
    public virtual void Update(float deltaTime)
    {
        // Override in derived classes for specific update logic
    }
    
    public virtual void Render(IGraphics2D renderer)
    {
        if (!Visible) 
            return;
        
        RenderBackground(renderer);
        RenderBorder(renderer);
        
        // Render content (override in derived classes)
        RenderContent(renderer);
        
        _isDirty = false;
    }
    
    
    protected virtual void RenderBackground(IGraphics2D renderer)
    {
        var backgroundColor = GetCurrentBackgroundColor();
        
        if (Style.BackgroundTexture != null)
        {
            renderer.DrawQuad(
                new Vector3(Position, 0), 
                Size, 
                Style.BackgroundTexture, 
                GetTextureCoords(),
                1.0f, 
                backgroundColor);
        }
        else if (backgroundColor.W > 0) // Only render if not fully transparent
        {
            renderer.DrawQuad(
                new Vector3(Position, 0), 
                Size, 
                backgroundColor);
        }
    }
    
    protected virtual void RenderBorder(IGraphics2D renderer)
    {
        if (Style.BorderWidth <= 0.0f) 
            return;
        
        var borderWidth = Style.BorderWidth;
        var borderColor = Style.BorderColor;
        
        // Top border
        renderer.DrawQuad(
            new Vector3(Position.X, Position.Y, 0), 
            Size with { Y = borderWidth }, 
            borderColor);
        
        // Bottom border
        renderer.DrawQuad(
            new Vector3(Position.X, Position.Y + Size.Y - borderWidth, 0), 
            Size with { Y = borderWidth }, 
            borderColor);
        
        // Left border
        renderer.DrawQuad(
            new Vector3(Position.X, Position.Y, 0), 
            Size with { X = borderWidth }, 
            borderColor);
        
        // Right border
        renderer.DrawQuad(
            new Vector3(Position.X + Size.X - borderWidth, Position.Y, 0), 
            Size with { X = borderWidth }, 
            borderColor);
    }
    
    protected abstract void RenderContent(IGraphics2D renderer);
    
    protected virtual Vector4 GetCurrentBackgroundColor() => Style.BackgroundColor;

    protected virtual Vector2[] GetTextureCoords()
    {
        return
        [
            new Vector2(0, 0), // Bottom-left
            new Vector2(1, 0), // Bottom-right
            new Vector2(1, 1), // Top-right
            new Vector2(0, 1)  // Top-left
        ];
    }
    
    public virtual bool ContainsPoint(Vector2 point)
    {
        return point.X >= Position.X && point.X <= Position.X + Size.X &&
               point.Y >= Position.Y && point.Y <= Position.Y + Size.Y;
    }
    
    public virtual void OnMouseClick(Vector2 mousePosition)
    {
        // Override in derived classes for click handling
    }
    
    public virtual void OnMouseHover(Vector2 mousePosition)
    {
        // Override in derived classes for hover handling
    }
    
    public virtual void OnMouseEnter(Vector2 mousePosition)
    {
        // Override in derived classes for mouse enter handling
    }
    
    public virtual void OnMouseExit(Vector2 mousePosition)
    {
        // Override in derived classes for mouse exit handling
    }
    
    public void MarkDirty()
    {
        _isDirty = true;
    }
    
    public bool IsDirty => _isDirty;
}

public struct Rectangle
{
    public Vector2 Position { get; }
    public Vector2 Size { get; }
    
    public Rectangle(Vector2 position, Vector2 size)
    {
        Position = position;
        Size = size;
    }
    
    public float Left => Position.X;
    public float Right => Position.X + Size.X;
    public float Top => Position.Y;
    public float Bottom => Position.Y + Size.Y;
    public Vector2 Center => Position + Size * 0.5f;
    
    public bool Contains(Vector2 point)
    {
        return point.X >= Left && point.X <= Right &&
               point.Y >= Top && point.Y <= Bottom;
    }
    
    public bool Intersects(Rectangle other)
    {
        return Left < other.Right && Right > other.Left &&
               Top < other.Bottom && Bottom > other.Top;
    }
}