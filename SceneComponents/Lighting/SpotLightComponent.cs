using System.Numerics;
using ECS;

namespace SceneComponents.Lighting;

public class SpotLightComponent : IComponent
{
    public Vector4 Color { get; set; } = Vector4.One;
    public float Constant { get; set; } = 1f;
    public float Linear { get; set; } = 0.09f;
    public float Quadratic { get; set; } = 0.032f;
    public Vector3 Direction { get; set; } = new(0, 0, -1);
    public float InnerCutoff { get; set; } = 12.5f;
    public float OuterCutoff { get; set; } = 17.5f;

    public IComponent Clone() => new SpotLightComponent
    {
        Color = Color,
        Constant = Constant,
        Linear = Linear,
        Quadratic = Quadratic,
        Direction = Direction,
        InnerCutoff = InnerCutoff,
        OuterCutoff = OuterCutoff
    };
}
