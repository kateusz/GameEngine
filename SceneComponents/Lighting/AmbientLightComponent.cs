using System.Numerics;
using ECS;

namespace SceneComponents.Lighting;

public class AmbientLightComponent : IComponent
{
    public Vector3 Color { get; set; } = Vector3.One;
    public float Strength { get; set; } = 0.35f;
    public IComponent Clone()
    {
        return new AmbientLightComponent
        {
            Color = Color,
            Strength = Strength
        };
    }
}