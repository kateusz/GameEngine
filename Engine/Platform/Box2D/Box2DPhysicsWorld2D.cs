using System.Numerics;
using Box2D.NetStandard.Dynamics.Bodies;
using Box2D.NetStandard.Dynamics.World;
using Engine.Physics;

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

    private static BodyType ToNativeBodyType(PhysicsBodyMotionType motionType) =>
        motionType switch
        {
            PhysicsBodyMotionType.Static => BodyType.Static,
            PhysicsBodyMotionType.Dynamic => BodyType.Dynamic,
            PhysicsBodyMotionType.Kinematic => BodyType.Kinematic,
            _ => throw new ArgumentOutOfRangeException(nameof(motionType), motionType, null)
        };
}
