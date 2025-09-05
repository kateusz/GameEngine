using System.Numerics;
using Engine.Renderer;
using Engine.UI.Rendering;

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
    private Font? _font;
    private FontRenderer? _fontRenderer;
    
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
    
    public Font? Font
    {
        get => _font;
        set
        {
            if (_font != value)
            {
                _font = value;
                MarkDirty();
                UpdateSizeFromContent();
            }
        }
    }
    
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
    
    public Text(string content, Font font) : this(content)
    {
        Font = font;
    }
    
    public Text(string content, Font font, TextAlignment alignment) : this(content, font)
    {
        Alignment = alignment;
    }
    
    protected override void RenderContent(IGraphics2D renderer)
    {
        if (string.IsNullOrEmpty(Content)) return;
        
        var font = GetEffectiveFont();
        if (font == null) return;
        
        var textColor = Style.TextColor;
        var scale = FontSize / font.Size;
        
        // Calculate text position based on alignment
        var textSize = MeasureText(Content, font, scale);
        var textPosition = CalculateTextPosition(textSize);
        
        // Render text using font renderer
        if (_fontRenderer != null)
        {
            _fontRenderer.RenderText(Content, textPosition, font, textColor, scale);
        }
    }
    
    
    private void UpdateSizeFromContent()
    {
        if (string.IsNullOrEmpty(Content))
        {
            Size = new Vector2(0, FontSize); // Height in screen coordinates
            return;
        }
        
        var font = GetEffectiveFont();
        if (font == null)
        {
            // Fallback to estimated size
            var charWidth = FontSize * 0.6f;
            var estimatedWidth = Content.Length * charWidth;
            var estimatedHeight = FontSize * 1.2f;
            Size = new Vector2(estimatedWidth, estimatedHeight);
            return;
        }
        
        var scale = FontSize / font.Size;
        var textSize = MeasureText(Content, font, scale);
        Size = textSize;
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
    
    public void SetFont(Font font)
    {
        Font = font;
    }
    
    public void SetFontRenderer(FontRenderer fontRenderer)
    {
        _fontRenderer = fontRenderer;
    }
    
    public Vector2 MeasureText(string text)
    {
        var font = GetEffectiveFont();
        if (font == null || _fontRenderer == null)
        {
            // Fallback estimation
            var charWidth = FontSize * 0.6f;
            var estimatedWidth = text.Length * charWidth;
            var estimatedHeight = FontSize * 1.2f;
            return new Vector2(estimatedWidth, estimatedHeight);
        }
        
        var scale = FontSize / font.Size;
        return _fontRenderer.MeasureText(text, font, scale);
    }
    
    private Font? GetEffectiveFont()
    {
        return Font ?? _fontRenderer?.GetDefaultFont();
    }
    
    private Vector2 CalculateTextPosition(Vector2 textSize)
    {
        var startX = Alignment switch
        {
            TextAlignment.Center => Position.X + (Size.X - textSize.X) * 0.5f,
            TextAlignment.Right => Position.X + Size.X - textSize.X,
            _ => Position.X
        };
        
        var startY = Position.Y + (Size.Y - textSize.Y) * 0.5f; // Vertical center
        
        return new Vector2(startX, startY);
    }
    
    private Vector2 MeasureText(string text, Font font, float scale)
    {
        if (_fontRenderer != null)
        {
            return _fontRenderer.MeasureText(text, font, scale);
        }
        
        // Fallback measurement
        var width = font.MeasureText(text) * scale;
        var height = font.LineHeight * scale;
        return new Vector2(width, height);
    }
}