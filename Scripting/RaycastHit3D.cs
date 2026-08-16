using System.Numerics;
using ECS;

namespace Scripting;

public readonly record struct RaycastHit3D(
    Entity Entity,
    Vector3 Point,
    Vector3 Normal,
    float Distance,
    bool IsTrigger);
