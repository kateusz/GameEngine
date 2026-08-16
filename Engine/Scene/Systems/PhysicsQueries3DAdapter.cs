using System.Numerics;
using ECS;
using Scripting;

namespace Engine.Scene.Systems;

/// <summary>
/// Exposes 3D physics queries through <see cref="IPhysicsQueries"/> for script injection.
/// 2D query methods intentionally miss — 3D scenes use <see cref="IPhysicsQueries3D"/> raycasts.
/// </summary>
internal sealed class PhysicsQueries3DAdapter(IPhysicsQueries3D queries3D) : IPhysicsQueries, IPhysicsQueries3D
{
    public RaycastHit2D? Raycast(
        Vector2 origin,
        Vector2 direction,
        float maxDistance,
        Entity? ignoreEntity = null,
        bool includeTriggers = false) => null;

    public RaycastHit2D? OverlapCircle(
        Vector2 center,
        float radius,
        Entity? ignoreEntity = null,
        bool includeTriggers = false) => null;

    public RaycastHit3D? Raycast(
        Vector3 origin,
        Vector3 direction,
        float maxDistance,
        Entity? ignoreEntity = null,
        bool includeTriggers = false) =>
        queries3D.Raycast(origin, direction, maxDistance, ignoreEntity, includeTriggers);

    public RaycastHit3D? OverlapSphere(
        Vector3 center,
        float radius,
        Entity? ignoreEntity = null,
        bool includeTriggers = false) =>
        queries3D.OverlapSphere(center, radius, ignoreEntity, includeTriggers);
}
