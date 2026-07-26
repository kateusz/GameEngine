using System.Numerics;

namespace Engine.Physics;

public readonly record struct PhysicsEdgeFixtureDef(
    Vector2[] Points,
    float Density,
    float Friction,
    float Restitution,
    bool IsSensor);
