using System.Numerics;
using ECS;

namespace SceneComponents.Lighting;

public class DirectionalLightComponent : IComponent
{
    public Vector3 Direction { get; set; } = new(0, -1, 0);
    public Vector3 Color { get; set; } = Vector3.One;

    public IComponent Clone() => new DirectionalLightComponent
    {
        Direction = Direction,
        Color = Color
    };
}
