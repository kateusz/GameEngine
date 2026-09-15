using System.Numerics;
using ECS;

namespace SceneComponents.Physics;

public class CircleCollider2DComponent : IComponent
{
    public float Radius { get; set; } = 0.5f;
    public Vector2 Offset { get; set; }
    public float Density { get; set; } = 1.0f;
    public float Friction { get; set; } = 0.5f;
    public float Restitution { get; set; } = 0.7f;
    public bool IsTrigger { get; set; }

    public IComponent Clone() => new CircleCollider2DComponent
    {
        Radius = Radius,
        Offset = Offset,
        Density = Density,
        Friction = Friction,
        Restitution = Restitution,
        IsTrigger = IsTrigger
    };
}
