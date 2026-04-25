using System.Numerics;
using ECS;

namespace Engine.Scene.Components.Lights;

public enum LightType { Point, Directional, Ambient }

public class PointLightComponent : IComponent
{
    public LightType Type { get; set; } = LightType.Point;
    public Vector3 Color { get; set; } = Vector3.One;
    public float Intensity { get; set; } = 10.0f; // 5.0f – 20.0f;

    public IComponent Clone() => new PointLightComponent
        { Type = Type, Color = Color, Intensity = Intensity };
}
