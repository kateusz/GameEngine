using System.Numerics;
using Box2D.NetStandard.Collision;
using Box2D.NetStandard.Collision.Shapes;
using Box2D.NetStandard.Dynamics.Bodies;
using Box2D.NetStandard.Dynamics.Fixtures;
using Box2D.NetStandard.Dynamics.World;
using ECS;
using Engine.Physics;
using SceneComponents.Physics;
using Scripting;

namespace Engine.Platform.Box2D;

internal sealed class Box2DPhysicsWorld2D : IPhysicsWorld2D
{
    private readonly World _world;
    private readonly Box2DContactListenerAdapter _contactListenerAdapter;
    private bool _disposed;

    public Box2DPhysicsWorld2D(Vector2 gravity, IPhysicsContactListener? listener = null)
    {
        _world = new World(gravity);
        _contactListenerAdapter = new Box2DContactListenerAdapter();
        _world.SetContactListener(_contactListenerAdapter);
        if (listener is not null)
            _contactListenerAdapter.SetListener(listener);
    }

    public void Step(float timeStep)
    {
        ThrowIfDisposed();
        _world.Step(timeStep, 6, 2);
    }

    public IPhysicsBody2D CreateBody(in PhysicsBodyDef def)
    {
        ThrowIfDisposed();
        var bodyDef = new BodyDef
        {
            position = def.Position,
            angle = def.Angle,
            type = ToNativeBodyType(def.MotionType),
            bullet = def.IsBullet,
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

    public (Entity Entity, bool IsTrigger)? OverlapCircle(
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

        (Entity Entity, bool IsTrigger)? hit = null;

        _world.QueryAABB(fixture =>
        {
            if (!TryResolveFixture(fixture, ignoreEntity, includeTriggers, out var entity, out var isTrigger))
                return true;

            if (!CircleOverlapsFixture(fixture, center, radius))
                return true;

            hit = (entity, isTrigger);
            return false;
        }, in aabb);

        return hit;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
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

    private static bool CircleOverlapsFixture(Fixture fixture, Vector2 center, float radius)
    {
        var body = fixture.GetBody();
        var radiusSq = radius * radius;
        switch (fixture.Shape)
        {
            case CircleShape circle:
                var combined = radius + circle.Radius;
                return Vector2.DistanceSquared(center, body.GetWorldPoint(circle.Center)) <= combined * combined;
            case PolygonShape poly:
                if (fixture.TestPoint(center))
                    return true;
                var verts = poly.GetVertices();
                for (var i = 0; i < verts.Length; i++)
                {
                    var a = body.GetWorldPoint(verts[i]);
                    var b = body.GetWorldPoint(verts[(i + 1) % verts.Length]);
                    if (DistanceSquaredToSegment(center, a, b) <= radiusSq)
                        return true;
                }
                return false;
            case EdgeShape edge:
                return DistanceSquaredToSegment(
                    center, body.GetWorldPoint(edge.Vertex1), body.GetWorldPoint(edge.Vertex2)) <= radiusSq;
            case ChainShape chain:
                for (var i = 0; i < chain.GetChildCount(); i++)
                {
                    chain.GetChildEdge(out var child, i);
                    if (DistanceSquaredToSegment(
                            center, body.GetWorldPoint(child.Vertex1), body.GetWorldPoint(child.Vertex2)) <= radiusSq)
                        return true;
                }
                return false;
            default:
                return true;
        }
    }

    private static float DistanceSquaredToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var lengthSq = ab.LengthSquared();
        if (lengthSq <= float.Epsilon)
            return Vector2.DistanceSquared(point, a);

        var t = float.Clamp(Vector2.Dot(point - a, ab) / lengthSq, 0f, 1f);
        return Vector2.DistanceSquared(point, a + ab * t);
    }

    private static BodyType ToNativeBodyType(RigidBodyType motionType) =>
        motionType switch
        {
            RigidBodyType.Static => BodyType.Static,
            RigidBodyType.Dynamic => BodyType.Dynamic,
            RigidBodyType.Kinematic => BodyType.Kinematic,
            _ => throw new ArgumentOutOfRangeException(nameof(motionType), motionType, null)
        };
}
