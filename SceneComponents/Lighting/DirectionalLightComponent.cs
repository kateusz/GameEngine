using System.Numerics;
using ECS;

namespace SceneComponents.Lighting;

public class DirectionalLightComponent : IComponent
{
    public Vector3 Direction { get; set; } = new(0, -1, 0);
    public Vector4 Color { get; set; } = Vector4.One;
    public float OrthoSize { get; set; } = 20f;

    public IComponent Clone() => new DirectionalLightComponent
    {
        Direction = Direction,
        Color = Color,
        OrthoSize = OrthoSize
    };
}
