using System.Numerics;
using ECS;

namespace Engine.Physics;

public interface IPhysicsBody2D
{
    Entity? Entity { get; set; }
    PhysicsBodyMotionType MotionType { get; }
    Vector2 Position { get; set; }
    float Angle { get; set; }
    Vector2 LinearVelocity { get; set; }
    bool FixedRotation { set; }
    bool HasFixture { get; }
    bool IsSensor { get; }
    bool IsEnabled();
    bool IsAwake();
    void CreateBoxFixture(in PhysicsBoxFixtureDef def);
    void CreateCircleFixture(in PhysicsCircleFixtureDef def);
    void CreateEdgeFixture(in PhysicsEdgeFixtureDef def);
    void UpdateFixtureMaterial(float density, float friction, float restitution);
}
