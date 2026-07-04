using Box2D.NetStandard.Collision;
using Box2D.NetStandard.Dynamics.Contacts;
using Box2D.NetStandard.Dynamics.World;
using Box2D.NetStandard.Dynamics.World.Callbacks;
using Engine.Physics;

namespace Engine.Platform.Box2D;

internal sealed class Box2DContactListenerAdapter : ContactListener
{
    private IPhysicsContactListener? _listener;

    public void SetListener(IPhysicsContactListener? listener) => _listener = listener;

    public override void BeginContact(in Contact contact)
    {
        if (_listener == null)
            return;

        var (bodyA, bodyB, isTrigger) = ResolveContact(contact);
        if (bodyA == null || bodyB == null)
            return;

        _listener.OnContactBegin(bodyA, bodyB, isTrigger);
    }

    public override void EndContact(in Contact contact)
    {
        if (_listener == null)
            return;

        var (bodyA, bodyB, isTrigger) = ResolveContact(contact);
        if (bodyA == null || bodyB == null)
            return;

        _listener.OnContactEnd(bodyA, bodyB, isTrigger);
    }

    public override void PreSolve(in Contact contact, in Manifold oldManifold)
    {
    }

    public override void PostSolve(in Contact contact, in ContactImpulse impulse)
    {
    }

    private static (IPhysicsBody2D? BodyA, IPhysicsBody2D? BodyB, bool IsTrigger) ResolveContact(in Contact contact)
    {
        var fixtureA = contact.GetFixtureA();
        var fixtureB = contact.GetFixtureB();
        var bodyA = fixtureA.GetBody().GetUserData<Box2DPhysicsBody2D>();
        var bodyB = fixtureB.GetBody().GetUserData<Box2DPhysicsBody2D>();
        var isTrigger = fixtureA.IsSensor() || fixtureB.IsSensor();
        return (bodyA, bodyB, isTrigger);
    }
}
