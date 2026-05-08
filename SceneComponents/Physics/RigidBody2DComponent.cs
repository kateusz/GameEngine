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

    public IComponent Clone()
    {
        return new RigidBody2DComponent
        {
            BodyType = BodyType,
            FixedRotation = FixedRotation
        };
    }
}