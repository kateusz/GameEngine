using System.Numerics;

namespace Engine.Physics;

public readonly record struct PhysicsBodyDef(
    Vector2 Position,
    float Angle,
    PhysicsBodyMotionType MotionType,
    bool FixedRotation,
    float GravityScale,
    bool IsBullet = false);
