using System.Numerics;

namespace Engine.Physics;

public readonly record struct PhysicsSphereFixtureDef3D(
    float Radius,
    Vector3 CenterOffset,
    float Density,
    float Friction,
    float Restitution,
    bool IsSensor);
