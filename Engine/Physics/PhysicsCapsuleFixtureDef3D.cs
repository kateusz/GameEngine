using System.Numerics;

namespace Engine.Physics;

/// <param name="Length">Cylindrical length between the hemisphere caps (Bepu convention).</param>
public readonly record struct PhysicsCapsuleFixtureDef3D(
    float Radius,
    float Length,
    Vector3 CenterOffset,
    float Density,
    float Friction,
    float Restitution,
    bool IsSensor);
