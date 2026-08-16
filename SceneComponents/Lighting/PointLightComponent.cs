using System.Numerics;
using ECS;

namespace SceneComponents.Lighting;

public class PointLightComponent : IComponent
{
    public Vector3 Color { get; set; } = Vector3.One;
    public float Strength { get; set; } = 1.0f;
    public float Range { get; set; } = 25.0f;

    public IComponent Clone() => new PointLightComponent
    {
        Color = Color,
        Strength = Strength,
        Range = Range
    };
}
