using System.Numerics;
using ECS;

namespace SceneComponents.Physics;

public class CapsuleCollider3DComponent : IComponent
{
    public float Radius { get; set; } = 0.5f;
    public float Length { get; set; } = 1f;
    public Vector3 Offset { get; set; }
    public float Density { get; set; } = 1f;
    public float Friction { get; set; } = 0.5f;
    public float Restitution { get; set; } = 0.7f;
    public bool IsTrigger { get; set; }

    public IComponent Clone() => new CapsuleCollider3DComponent
    {
        Radius = Radius,
        Length = Length,
        Offset = Offset,
        Density = Density,
        Friction = Friction,
        Restitution = Restitution,
        IsTrigger = IsTrigger
    };
}
