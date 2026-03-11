using System.Numerics;
using ECS;

namespace Engine.Scene.Components;

public enum LightType
{
    Directional = 0,
    Point = 1,
    Spot = 2
}

public class LightComponent : IComponent
{
    public LightType Type { get; set; } = LightType.Directional;
    public Vector3 Color { get; set; } = Vector3.One;
    public float Intensity { get; set; } = 1.0f;

    /// <summary>
    /// Direction for directional and spot lights. Ignored for point lights.
    /// </summary>
    public Vector3 Direction { get; set; } = new(0.0f, -1.0f, 0.0f);

    /// <summary>
    /// Attenuation range for point and spot lights.
    /// </summary>
    public float Range { get; set; } = 10.0f;

    /// <summary>
    /// Inner cone angle in degrees for spot lights.
    /// </summary>
    public float InnerConeAngle { get; set; } = 12.5f;

    /// <summary>
    /// Outer cone angle in degrees for spot lights.
    /// </summary>
    public float OuterConeAngle { get; set; } = 17.5f;

    /// <summary>
    /// Whether this light casts shadows.
    /// </summary>
    public bool CastShadows { get; set; } = true;

    public IComponent Clone()
    {
        return new LightComponent
        {
            Type = Type,
            Color = Color,
            Intensity = Intensity,
            Direction = Direction,
            Range = Range,
            InnerConeAngle = InnerConeAngle,
            OuterConeAngle = OuterConeAngle,
            CastShadows = CastShadows
        };
    }
}
