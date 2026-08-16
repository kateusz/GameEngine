using System.Numerics;

namespace Engine.Physics;

public readonly record struct PhysicsBodyDef3D(
    Vector3 Position,
    Quaternion Orientation,
    PhysicsBodyMotionType MotionType,
    bool FixedRotation,
    float GravityScale);
