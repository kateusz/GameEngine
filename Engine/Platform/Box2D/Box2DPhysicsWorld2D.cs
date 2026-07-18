using System.Numerics;
using Box2D.NetStandard.Collision;
using Box2D.NetStandard.Dynamics.Bodies;
using Box2D.NetStandard.Dynamics.Fixtures;
using Box2D.NetStandard.Dynamics.World;
using ECS;
using Engine.Physics;
using Scripting;

namespace Engine.Platform.Box2D;

internal sealed class Box2DPhysicsWorld2D : IPhysicsWorld2D
{
    private readonly World _world;
    private readonly Box2DContactListenerAdapter _contactListenerAdapter;
    private bool _disposed;

    public Box2DPhysicsWorld2D(Vector2 gravity)
    {
        _world = new World(gravity);
        _contactListenerAdapter = new Box2DContactListenerAdapter();
        _world.SetContactListener(_contactListenerAdapter);
    }

    public void Step(float timeStep, int velocityIterations, int positionIterations)
    {
        ThrowIfDisposed();
        _world.Step(timeStep, velocityIterations, positionIterations);
    }

    public IPhysicsBody2D CreateBody(in PhysicsBodyDef def)
    {
        ThrowIfDisposed();
        var bodyDef = new BodyDef
        {
            position = def.Position,
            angle = def.Angle,
            type = ToNativeBodyType(def.MotionType),
            bullet = def.MotionType == PhysicsBodyMotionType.Dynamic,
            gravityScale = def.GravityScale
        };

        var body = _world.CreateBody(bodyDef);
        body.SetFixedRotation(def.FixedRotation);
        var wrapper = new Box2DPhysicsBody2D(body);
        body.SetUserData(wrapper);
        return wrapper;
    }

    public void DestroyBody(IPhysicsBody2D body)
    {
        ThrowIfDisposed();
        if (body is not Box2DPhysicsBody2D box2DBody)
            return;

        box2DBody.Entity = null;
        box2DBody.NativeBody.SetUserData(null);
        _world.DestroyBody(box2DBody.NativeBody);
    }

    public void SetContactListener(IPhysicsContactListener? listener)
    {
        ThrowIfDisposed();
        _contactListenerAdapter.SetListener(listener);
    }

    public RaycastHit2D? Raycast(
        Vector2 origin,
        Vector2 direction,
        float maxDistance,
        Entity? ignoreEntity = null,
        bool includeTriggers = false)
    {
        ThrowIfDisposed();
        if (!IsValidRay(origin, direction, maxDistance))
            return null;

        var normalized = Vector2.Normalize(direction);
        var end = origin + normalized * maxDistance;

        RaycastHit2D? closest = null;
        var closestFraction = float.MaxValue;

        _world.RayCast((fixture, point, normal, fraction) =>
        {
            if (!TryResolveFixture(fixture, ignoreEntity, includeTriggers, out var entity, out var isTrigger))
                return;

            if (fraction >= closestFraction)
                return;

            closestFraction = fraction;
            closest = new RaycastHit2D(entity, point, normal, fraction * maxDistance, isTrigger);
        }, origin, end);

        return closest;
    }

    public RaycastHit2D? OverlapCircle(
        Vector2 center,
        float radius,
        Entity? ignoreEntity = null,
        bool includeTriggers = false)
    {
        ThrowIfDisposed();
        if (!IsValidCircle(center, radius))
            return null;

        var aabb = new AABB(
            new Vector2(center.X - radius, center.Y - radius),
            new Vector2(center.X + radius, center.Y + radius));

        RaycastHit2D? hit = null;

        _world.QueryAABB(fixture =>
        {
            if (!TryResolveFixture(fixture, ignoreEntity, includeTriggers, out var entity, out var isTrigger))
                return true;

            hit = new RaycastHit2D(entity, center, Vector2.Zero, 0f, isTrigger);
            return false;
        }, in aabb);

        return hit;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Box2DPhysicsWorld2D));
    }

    private static bool IsValidRay(Vector2 origin, Vector2 direction, float maxDistance) =>
        maxDistance > 0f
        && float.IsFinite(maxDistance)
        && float.IsFinite(origin.X) && float.IsFinite(origin.Y)
        && float.IsFinite(direction.X) && float.IsFinite(direction.Y)
        && direction.LengthSquared() > float.Epsilon;

    private static bool IsValidCircle(Vector2 center, float radius) =>
        radius > 0f
        && float.IsFinite(radius)
        && float.IsFinite(center.X) && float.IsFinite(center.Y);

    private static bool TryResolveFixture(
        Fixture fixture,
        Entity? ignoreEntity,
        bool includeTriggers,
        out Entity entity,
        out bool isTrigger)
    {
        entity = null!;
        isTrigger = fixture.IsSensor();

        var wrapper = fixture.GetBody().GetUserData<Box2DPhysicsBody2D>();
        if (wrapper?.Entity is not { } resolvedEntity)
            return false;

        if (ignoreEntity is not null && resolvedEntity.Id == ignoreEntity.Id)
            return false;

        if (isTrigger && !includeTriggers)
            return false;

        entity = resolvedEntity;
        return true;
    }

    private static BodyType ToNativeBodyType(PhysicsBodyMotionType motionType) =>
        motionType switch
        {
            PhysicsBodyMotionType.Static => BodyType.Static,
            PhysicsBodyMotionType.Dynamic => BodyType.Dynamic,
            PhysicsBodyMotionType.Kinematic => BodyType.Kinematic,
            _ => throw new ArgumentOutOfRangeException(nameof(motionType), motionType, null)
        };
}
