using System.Numerics;
using ECS;

namespace Scripting;

public readonly record struct RaycastHit2D(
    Entity Entity,
    Vector2 Point,
    Vector2 Normal,
    float Distance,
    bool IsTrigger);
