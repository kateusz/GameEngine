using System.Numerics;
using ECS;

namespace Scripting;

public interface IPhysicsQueries
{
    RaycastHit2D? Raycast(
        Vector2 origin,
        Vector2 direction,
        float maxDistance,
        Entity? ignoreEntity = null,
        bool includeTriggers = false);

    RaycastHit2D? OverlapCircle(
        Vector2 center,
        float radius,
        Entity? ignoreEntity = null,
        bool includeTriggers = false);
}
