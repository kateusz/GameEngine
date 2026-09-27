using System.Numerics;
using ECS;

namespace SceneComponents.Rendering;

public class VisibilityZoneComponent : IComponent
{
    public Vector3 Min { get; set; } = new(-0.5f);
    public Vector3 Max { get; set; } = new(0.5f);

    public IComponent Clone() => new VisibilityZoneComponent
    {
        Min = Min,
        Max = Max
    };
}
