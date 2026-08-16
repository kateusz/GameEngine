using System.Numerics;
using ECS;
using Engine.Core;
using Scripting;

namespace Engine.Scene.Systems;

[SkipUnitTests]
internal sealed class NullPhysicsQueries3D : IPhysicsQueries3D
{
    public static readonly NullPhysicsQueries3D Instance = new();

    public RaycastHit3D? Raycast(
        Vector3 origin,
        Vector3 direction,
        float maxDistance,
        Entity? ignoreEntity = null,
        bool includeTriggers = false) => null;

    public RaycastHit3D? OverlapSphere(
        Vector3 center,
        float radius,
        Entity? ignoreEntity = null,
        bool includeTriggers = false) => null;
}
