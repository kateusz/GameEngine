using System.Numerics;
using ECS;

namespace SceneComponents.Rendering;

public class ModelRendererComponent : IComponent
{
    public Vector4 Color { get; set; } = Vector4.One;
    public string? TexturePath { get; set; }
    public float TilingFactor { get; set; } = 1.0f;
    public string? ModelPath { get; set; }

    public ModelRendererComponent() { }

    public ModelRendererComponent(Vector4 color)
    {
        Color = color;
    }

    public IComponent Clone() => new ModelRendererComponent
    {
        Color = Color,
        TexturePath = TexturePath,
        TilingFactor = TilingFactor,
        ModelPath = ModelPath
    };
}