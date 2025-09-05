using System.Numerics;
using Engine.Renderer.Textures;

namespace Engine.UI;

public class UIStyle
{
    public Vector4 BackgroundColor { get; set; } = new(0.2f, 0.2f, 0.2f, 1.0f);
    public Vector4 TextColor { get; set; } = Vector4.One;
    public Texture2D? BackgroundTexture { get; set; } = null;
    public float BorderWidth { get; set; } = 0.0f;
    public Vector4 BorderColor { get; set; } = Vector4.One;
    
    public Vector4 HoverBackgroundColor { get; set; } = new(0.3f, 0.3f, 0.3f, 1.0f);
    public Vector4 PressedBackgroundColor { get; set; } = new(0.15f, 0.15f, 0.15f, 1.0f);
    public Vector4 DisabledBackgroundColor { get; set; } = new(0.1f, 0.1f, 0.1f, 0.5f);
    public Vector4 DisabledTextColor { get; set; } = new(0.5f, 0.5f, 0.5f, 1.0f);

    public UIStyle Clone()
    {
        return new UIStyle
        {
            BackgroundColor = BackgroundColor,
            TextColor = TextColor,
            BackgroundTexture = BackgroundTexture,
            BorderWidth = BorderWidth,
            BorderColor = BorderColor,
            HoverBackgroundColor = HoverBackgroundColor,
            PressedBackgroundColor = PressedBackgroundColor,
            DisabledBackgroundColor = DisabledBackgroundColor,
            DisabledTextColor = DisabledTextColor
        };
    }

    public static UIStyle Default => new();
    
    public static UIStyle Button => new()
    {
        BackgroundColor = new Vector4(0.25f, 0.35f, 0.55f, 1.0f),
        TextColor = Vector4.One,
        HoverBackgroundColor = new Vector4(0.35f, 0.45f, 0.65f, 1.0f),
        PressedBackgroundColor = new Vector4(0.15f, 0.25f, 0.45f, 1.0f),
        BorderWidth = 1.0f,
        BorderColor = new Vector4(0.4f, 0.5f, 0.7f, 1.0f)
    };
    
    public static UIStyle Text => new()
    {
        BackgroundColor = new Vector4(0, 0, 0, 0), // Fully transparent
        TextColor = Vector4.One
    };
}