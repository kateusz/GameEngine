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
    public RigidBodyType BodyType { get; set; }
    public bool FixedRotation { get; set; }
    public float GravityScale { get; set; }
    public Vector2 Velocity { get; set; }

    public RigidBody2DComponent()
    {
        GravityScale = 1f;
    }

    public IComponent Clone()
    {
        return new RigidBody2DComponent
        {
            BodyType = BodyType,
            FixedRotation = FixedRotation,
            GravityScale = GravityScale,
            Velocity = Velocity
        };
    }
}