using System.Numerics;

namespace Engine.Physics;

public readonly record struct PhysicsCircleFixtureDef(
    float Radius,
    Vector2 CenterOffset,
    float Density,
    float Friction,
    float Restitution,
    bool IsSensor);
