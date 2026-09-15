using System.Numerics;
using SceneComponents.Physics;

namespace Engine.Physics;

public readonly record struct PhysicsBodyDef(
    Vector2 Position,
    float Angle,
    RigidBodyType MotionType,
    bool FixedRotation,
    float GravityScale,
    bool IsBullet = false);
