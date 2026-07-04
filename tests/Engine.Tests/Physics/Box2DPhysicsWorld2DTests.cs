using System.Numerics;
using Engine.Physics;
using Engine.Platform.Box2D;
using Shouldly;

namespace Engine.Tests.Physics;

public class Box2DPhysicsWorld2DTests
{
    [Fact]
    public void CreateBody_Step_UpdatesDynamicBodyPosition()
    {
        using var world = new Box2DPhysicsWorld2D(new Vector2(0, -10f));
        var body = world.CreateBody(new PhysicsBodyDef(
            Vector2.Zero,
            0f,
            PhysicsBodyMotionType.Dynamic,
            FixedRotation: false,
            GravityScale: 1f));

        body.CreateBoxFixture(new PhysicsBoxFixtureDef(0.5f, 0.5f, Vector2.Zero, 1f, 0.5f, 0f, false));

        var startY = body.Position.Y;
        world.Step(1f / 60f, 6, 2);

        body.Position.Y.ShouldBeLessThan(startY);
    }

    [Fact]
    public void CreateBody_AppliesFixedRotation()
    {
        using var world = new Box2DPhysicsWorld2D(Vector2.Zero);

        var fixedBody = world.CreateBody(new PhysicsBodyDef(
            Vector2.Zero,
            0f,
            PhysicsBodyMotionType.Dynamic,
            FixedRotation: true,
            GravityScale: 0f));
        fixedBody.CreateBoxFixture(new PhysicsBoxFixtureDef(0.5f, 0.5f, Vector2.Zero, 1f, 0.5f, 0f, false));
        ((Box2DPhysicsBody2D)fixedBody).NativeBody.ApplyAngularImpulse(10f);

        var freeBody = world.CreateBody(new PhysicsBodyDef(
            Vector2.Zero,
            0f,
            PhysicsBodyMotionType.Dynamic,
            FixedRotation: false,
            GravityScale: 0f));
        freeBody.CreateBoxFixture(new PhysicsBoxFixtureDef(0.5f, 0.5f, Vector2.Zero, 1f, 0.5f, 0f, false));
        ((Box2DPhysicsBody2D)freeBody).NativeBody.ApplyAngularImpulse(10f);

        for (var i = 0; i < 10; i++)
            world.Step(1f / 60f, 6, 2);

        fixedBody.Angle.ShouldBe(0f);
        freeBody.Angle.ShouldNotBe(0f);
    }

    [Fact]
    public void PhysicsWorld2DFactory_Box2DBackend_CreatesWorld()
    {
        IPhysicsWorld2DFactory factory = new PhysicsWorld2DFactory(new PhysicsBackendConfig(PhysicsBackendType.Box2D));

        using var world = factory.Create(new Vector2(0, -9.8f));
        var body = world.CreateBody(new PhysicsBodyDef(
            new Vector2(1f, 2f),
            0.5f,
            PhysicsBodyMotionType.Static,
            FixedRotation: true,
            GravityScale: 0f));

        body.Position.ShouldBe(new Vector2(1f, 2f));
        body.Angle.ShouldBe(0.5f);
        body.MotionType.ShouldBe(PhysicsBodyMotionType.Static);
    }

    [Fact]
    public void Step_AfterDispose_ThrowsObjectDisposedException()
    {
        var world = new Box2DPhysicsWorld2D(Vector2.Zero);
        world.Dispose();

        Should.Throw<ObjectDisposedException>(() => world.Step(1f / 60f, 6, 2));
    }
}
