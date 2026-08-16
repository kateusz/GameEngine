using System.Numerics;
using ECS;

namespace SceneComponents.Physics;

public class RigidBody3DComponent : IComponent
{
    public RigidBodyType BodyType { get; set; }
    public bool FixedRotation { get; set; }
    public float GravityScale { get; set; } = 1f;
    public Vector3 Velocity { get; set; }

    public IComponent Clone() => new RigidBody3DComponent
    {
        BodyType = BodyType,
        FixedRotation = FixedRotation,
        GravityScale = GravityScale,
        Velocity = Velocity
    };
}
