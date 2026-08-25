using System.Numerics;
using ECS;

namespace SceneComponents.Lighting;

public class PointLightComponent : IComponent
{
    public Vector4 Color { get; set; } = Vector4.One;
    public float Constant { get; set; } = 1f;
    public float Linear { get; set; } = 0.09f;
    public float Quadratic { get; set; } = 0.032f;
    public float Range { get; set; } = 25f;

    public IComponent Clone() => new PointLightComponent
    {
        Color = Color,
        Constant = Constant,
        Linear = Linear,
        Quadratic = Quadratic,
        Range = Range
    };
}
