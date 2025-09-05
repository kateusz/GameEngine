using System;
using System.Numerics;
using Engine.Renderer;

namespace Engine.UI.Elements;

public enum TextAlignment
{
    Left,
    Center,
    Right
}

public class Text : UIElement
{
    private string _content = string.Empty;
    private TextAlignment _alignment = TextAlignment.Left;
    private float _fontSize = 16.0f;
    
    public string Content
    {
        get => _content;
        set
        {
            if (_content != value)
            {
                _content = value;
                MarkDirty();
                UpdateSizeFromContent();
            }
        }
    }
    
    public TextAlignment Alignment
    {
        get => _alignment;
        set
        {
            if (_alignment != value)
            {
                _alignment = value;
                MarkDirty();
            }
        }
    }
    
    public float FontSize
    {
        get => _fontSize;
        set
        {
            if (System.Math.Abs(_fontSize - value) > 0.001f)
            {
                _fontSize = value;
                MarkDirty();
                UpdateSizeFromContent();
            }
        }
    }
    
    // TODO: Add Font property when font system is implemented
    // public Font Font { get; set; }
    
    public Text() : this(string.Empty)
    {
    }
    
    public Text(string content)
    {
        Content = content;
        Style = UIStyle.Text.Clone();
        Interactive = false; // Text is typically non-interactive
        UpdateSizeFromContent();
    }
    
    public Text(string content, TextAlignment alignment) : this(content)
    {
        Alignment = alignment;
    }
    
    protected override void RenderContent(IGraphics2D renderer)
    {
        if (string.IsNullOrEmpty(Content)) return;
        
        // Placeholder text rendering using colored quads
        // This will be replaced with proper glyph rendering in Phase 2
        
        var textColor = Style.TextColor;
        var charWidth = FontSize * 0.6f; // Approximate character width
        var charHeight = FontSize;
        var totalTextWidth = Content.Length * charWidth;
        
        // Calculate starting position based on alignment
        var startX = Alignment switch
        {
            TextAlignment.Center => Position.X + (Size.X - totalTextWidth) * 0.5f,
            TextAlignment.Right => Position.X + Size.X - totalTextWidth,
            _ => Position.X
        };
        
        var startY = Position.Y + (Size.Y - charHeight) * 0.5f; // Vertical center
        
        // Render each character as a small colored quad (placeholder)
        for (int i = 0; i < Content.Length; i++)
        {
            var char_c = Content[i];
            if (char_c == ' ') continue; // Skip spaces
            
            var charPosition = new Vector3(startX + i * charWidth, startY, 0);
            var charSize = new Vector2(charWidth * 0.8f, charHeight);
            
            // Vary the color slightly based on character for visual distinction
            var charColor = textColor;
            if (char.IsLetter(char_c))
            {
                // Slightly different shades for letters vs other characters
                charColor = textColor with { X = textColor.X * 0.9f };
            }
            
            renderer.DrawQuad(charPosition, charSize, charColor);
        }
    }
    
    
    private void UpdateSizeFromContent()
    {
        if (string.IsNullOrEmpty(Content))
        {
            Size = new Vector2(0, FontSize);
            return;
        }
        
        // Estimate text size based on content
        // This will be replaced with proper text measurement in Phase 2
        var charWidth = FontSize * 0.6f;
        var estimatedWidth = Content.Length * charWidth;
        var estimatedHeight = FontSize * 1.2f; // Add some line height
        
        Size = new Vector2(estimatedWidth, estimatedHeight);
    }
    
    public void SetContent(string content)
    {
        Content = content;
    }
    
    public void SetAlignment(TextAlignment alignment)
    {
        Alignment = alignment;
    }
    
    public void SetFontSize(float fontSize)
    {
        FontSize = fontSize;
    }
    
    // TODO: Methods to be implemented in Phase 2 with font system
    // public Vector2 MeasureText(string text)
    // public void SetFont(Font font)
}