using System;
using System.Numerics;
using Engine.Renderer;

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
        // For now, just render a simple text placeholder in the center
        // This will be replaced with proper text rendering in Phase 2
        if (string.IsNullOrEmpty(Text)) 
            return;
        
        var textColor = GetCurrentTextColor();
        var textPosition = Position + Size * 0.5f; // Center of button
            
        // Placeholder: render a small colored quad to represent text
        // This will be replaced with actual font rendering
        var textSize = new Vector2(System.Math.Min(Size.X - 20, Text.Length * 8), 12);
        var textPos = textPosition - textSize * 0.5f;
            
        renderer.DrawQuad(
            new Vector3(textPos, 0), 
            textSize, 
            textColor);
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
}