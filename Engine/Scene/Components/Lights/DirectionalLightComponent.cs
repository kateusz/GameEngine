using System.Numerics;
using ECS;

namespace Engine.Scene.Components.Lights;

public class DirectionalLightComponent : IComponent
{
    public LightType Type { get; set; } = LightType.Directional;
    public Vector3 Direction { get; set; } = new(0f, -1f, 0f);
    public Vector3 Color { get; set; } = new(0.2f, 0.25f, 0.3f);
    public float Strength { get; set; } = 0.3f;

    public IComponent Clone() => new DirectionalLightComponent
        { Type = Type, Direction = Direction, Strength = Strength, Color = Color };
}
