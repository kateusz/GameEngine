using System.Numerics;
using ECS;

namespace SceneComponents.Physics;

public enum RigidBodyType
{
    Static,
    Dynamic,
    Kinematic
}

public class RigidBody2DComponent : IComponent
{
    public RigidBodyType BodyType
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

    public bool FixedRotation
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

    public float GravityScale
    {
        get;
        set
        {
            if (field.Equals(value))
                return;
            field = value;
            PhysicsBodyRevision.Bump();
        }
    } = 1f;

    public bool IsBullet
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

    public Vector2 Velocity { get; set; }

    public IComponent Clone()
    {
        return new RigidBody2DComponent
        {
            BodyType = BodyType,
            FixedRotation = FixedRotation,
            GravityScale = GravityScale,
            IsBullet = IsBullet,
            Velocity = Velocity
        };
    }
}
