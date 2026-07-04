using System.Numerics;

namespace Engine.Physics;

public readonly record struct PhysicsBoxFixtureDef(
    float HalfWidth,
    float HalfHeight,
    Vector2 CenterOffset,
    float Density,
    float Friction,
    float Restitution,
    bool IsSensor);
