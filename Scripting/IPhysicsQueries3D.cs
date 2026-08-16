using System.Numerics;
using ECS;

namespace Scripting;

public interface IPhysicsQueries3D
{
    RaycastHit3D? Raycast(
        Vector3 origin,
        Vector3 direction,
        float maxDistance,
        Entity? ignoreEntity = null,
        bool includeTriggers = false);

    RaycastHit3D? OverlapSphere(
        Vector3 center,
        float radius,
        Entity? ignoreEntity = null,
        bool includeTriggers = false);
}
