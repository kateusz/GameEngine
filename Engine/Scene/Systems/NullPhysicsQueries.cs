using System.Numerics;
using ECS;
using Engine.Core;
using Scripting;

namespace Engine.Scene.Systems;

[SkipUnitTests]
internal sealed class NullPhysicsQueries : IPhysicsQueries
{
    public static readonly NullPhysicsQueries Instance = new();

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
}
