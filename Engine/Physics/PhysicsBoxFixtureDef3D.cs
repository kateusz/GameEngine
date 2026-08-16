using System.Numerics;

namespace Engine.Physics;

public readonly record struct PhysicsBoxFixtureDef3D(
    Vector3 HalfExtents,
    Vector3 CenterOffset,
    float Density,
    float Friction,
    float Restitution,
    bool IsSensor);
