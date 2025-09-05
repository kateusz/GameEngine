using System.Numerics;
using Engine.Renderer;
using Engine.UI.Rendering;

namespace Engine.UI.Elements;

public enum ButtonState
{
    Normal,
    Hover,
    Pressed,
    Disabled
}

public class Button : UIElement
{
    private ButtonState _state = ButtonState.Normal;
    private bool _isHovering = false;
    private bool _isPressed = false;
    private Font? _font;
    private FontRenderer? _fontRenderer;
    
    public string Text { get; set; } = string.Empty;
    public Action? OnClick { get; set; }
    public Action? OnHover { get; set; }
    public Action? OnPress { get; set; }
    public Action? OnRelease { get; set; }
    
    public ButtonState State
    {
        get => _state;
        set
        {
            if (_state != value)
            {
                _state = value;
                MarkDirty();
            }
        }
    }
    
    public bool Enabled
    {
        get => State != ButtonState.Disabled;
        set => State = value ? ButtonState.Normal : ButtonState.Disabled;
    }
    
    public Button(string text)
    {
        Text = text;
        Style = UIStyle.Button.Clone();
        Interactive = true;
        Size = new Vector2(120, 40);
    }
    
    public Button(string text, Action onClick) : this(text)
    {
        OnClick = onClick;
    }
    
    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        
        // Update button state based on interaction
        if (!Enabled)
        {
            State = ButtonState.Disabled;
        }
        else if (_isPressed)
        {
            State = ButtonState.Pressed;
        }
        else if (_isHovering)
        {
            State = ButtonState.Hover;
        }
        else
        {
            State = ButtonState.Normal;
        }
    }
    
    protected override void RenderContent(IGraphics2D renderer)
    {
        if (string.IsNullOrEmpty(Text)) 
            return;
        
        var font = GetEffectiveFont();
        if (font == null) return;
        
        var textColor = GetCurrentTextColor();
        var scale = 1.0f; // Button text scale
        
        // Calculate text position (centered in button)
        var textSize = MeasureText(Text, font, scale);
        var textPosition = Position + (Size - textSize) * 0.5f;
        
        // Render text using font renderer
        if (_fontRenderer != null)
        {
            _fontRenderer.RenderText(Text, textPosition, font, textColor, scale);
        }
    }
    
    
    protected override Vector4 GetCurrentBackgroundColor()
    {
        return State switch
        {
            ButtonState.Hover => Style.HoverBackgroundColor,
            ButtonState.Pressed => Style.PressedBackgroundColor,
            ButtonState.Disabled => Style.DisabledBackgroundColor,
            _ => Style.BackgroundColor
        };
    }
    
    private Vector4 GetCurrentTextColor()
    {
        return State switch
        {
            ButtonState.Disabled => Style.DisabledTextColor,
            _ => Style.TextColor
        };
    }
    
    public override void OnMouseClick(Vector2 mousePosition)
    {
        if (!Enabled || !Interactive) return;
        
        _isPressed = true;
        OnPress?.Invoke();
        OnClick?.Invoke();
        MarkDirty();
    }
    
    public override void OnMouseHover(Vector2 mousePosition)
    {
        if (!Enabled || !Interactive) return;
        
        if (!_isHovering)
        {
            _isHovering = true;
            OnHover?.Invoke();
            MarkDirty();
        }
    }
    
    public override void OnMouseEnter(Vector2 mousePosition)
    {
        if (!Enabled || !Interactive) return;
        
        _isHovering = true;
        OnHover?.Invoke();
        MarkDirty();
    }
    
    public override void OnMouseExit(Vector2 mousePosition)
    {
        _isHovering = false;
        _isPressed = false;
        OnRelease?.Invoke();
        MarkDirty();
    }
    
    public void SetText(string text)
    {
        if (Text != text)
        {
            Text = text;
            MarkDirty();
        }
    }
    
    public void Click()
    {
        if (Enabled)
        {
            OnClick?.Invoke();
        }
    }
    
    public void SetFont(Font font)
    {
        _font = font;
        MarkDirty();
    }
    
    public void SetFontRenderer(FontRenderer fontRenderer)
    {
        _fontRenderer = fontRenderer;
    }
    
    private Font? GetEffectiveFont()
    {
        return _font ?? _fontRenderer?.GetDefaultFont();
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