using System.Numerics;
using ECS;

namespace SceneComponents.Physics;

public class CircleCollider2DComponent : IComponent
{
    public float Radius
    {
        get;
        set
        {
            if (field.Equals(value))
                return;
            field = value;
            PhysicsBodyRevision.Bump();
        }
    } = 0.5f;

    public Vector2 Offset
    {
        get;
        set
        {
            if (field == value)
                return;
            field = value;
            PhysicsBodyRevision.Bump();
        }
    }

    public float Density
    {
        get;
        set
        {
            if (field.Equals(value))
                return;
            field = value;
            PhysicsBodyRevision.Bump();
        }
    } = 1.0f;

    public float Friction { get; set; } = 0.5f;
    public float Restitution { get; set; } = 0.7f;

    public bool IsTrigger
    {
        get;
        set
        {
            if (field == value)
                return;
            field = value;
            PhysicsBodyRevision.Bump();
        }
    }

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
