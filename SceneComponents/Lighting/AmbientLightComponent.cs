using System.Numerics;
using ECS;

namespace SceneComponents.Lighting;

public class AmbientLightComponent : IComponent
{
    public Vector4 Color { get; set; } = Vector4.One;
    public float Strength { get; set; } = 0.1f;
    public IComponent Clone()
    {
        return new AmbientLightComponent
        {
            Color = Color,
            Strength = Strength
        };
    }
}