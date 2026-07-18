using System.Numerics;
using ECS;

namespace SceneComponents.Rendering;

public class ModelRendererComponent : IComponent
{
    public string? ModelPath { get; set; }
    public Vector4 Color { get; set; } = Vector4.One;
    public float? MetallicOverride { get; set; }
    public float? RoughnessOverride { get; set; }

    public ModelRendererComponent() { }

    public ModelRendererComponent(Vector4 color)
    {
        Color = color;
    }

    public IComponent Clone() => new ModelRendererComponent
    {
        ModelPath = ModelPath,
        Color = Color,
        MetallicOverride = MetallicOverride,
        RoughnessOverride = RoughnessOverride
    };
}
