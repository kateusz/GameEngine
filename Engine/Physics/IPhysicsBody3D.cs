using System.Numerics;
using ECS;

namespace Engine.Physics;

public interface IPhysicsBody3D
{
    Entity? Entity { get; set; }
    PhysicsBodyMotionType MotionType { get; }
    Vector3 Position { get; set; }
    Quaternion Orientation { get; set; }
    Vector3 LinearVelocity { get; set; }
    bool FixedRotation { set; }
    bool HasFixture { get; }
    bool IsSensor { get; }
    bool IsEnabled();
    bool IsAwake();
    void CreateBoxFixture(in PhysicsBoxFixtureDef3D def);
    void CreateSphereFixture(in PhysicsSphereFixtureDef3D def);
    void CreateCapsuleFixture(in PhysicsCapsuleFixtureDef3D def);
    void UpdateFixtureMaterial(float density, float friction, float restitution);
}
