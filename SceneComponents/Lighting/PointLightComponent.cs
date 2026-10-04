using System.Numerics;
using ECS;

namespace SceneComponents.Lighting;

public class PointLightComponent : IComponent
{
    public Vector4 Color { get; set; } = Vector4.One;
    public float Intensity { get; set; } = 1f;
    public float Range { get; set; } = 10f;
    public bool CastsShadow { get; set; }

    public IComponent Clone() => new PointLightComponent
    {
        Color = Color,
        Intensity = Intensity,
        Range = Range,
        CastsShadow = CastsShadow
    };
}