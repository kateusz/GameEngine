using System.Numerics;
using ECS;

namespace Engine.Scene.Components.Lights;

public class AmbientLightComponent : IComponent
{
    public LightType Type { get; set; } = LightType.Ambient;
    public Vector3 Color { get; set; } = Vector3.One;
    public float Strength { get; set; } = 0.1f;

    public IComponent Clone() => new AmbientLightComponent
        { Type = Type, Color = Color, Strength = Strength };
}
