using System.Numerics;
using ECS;
using Engine.Physics;
using Engine.Platform.Box2D;
using Shouldly;

namespace Engine.Tests.Physics;

public class Box2DPhysicsWorld2DTests
{
    [Fact]
    public void Step_ContactBegin_ResolvesBodiesAfterEntityAssignment()
    {
        using var world = new Box2DPhysicsWorld2D(new Vector2(0, -20f));
        var contactCount = 0;
        world.SetContactListener(new RecordingContactListener(() => contactCount++));

        var floor = world.CreateBody(new PhysicsBodyDef(
            new Vector2(0, -2f),
            0f,
            PhysicsBodyMotionType.Static,
            FixedRotation: false,
            GravityScale: 0f));
        floor.Entity = Entity.Create(1, "Floor");
        floor.CreateBoxFixture(new PhysicsBoxFixtureDef(5f, 0.5f, Vector2.Zero, 0f, 0.5f, 0f, false));

        var ball = world.CreateBody(new PhysicsBodyDef(
            new Vector2(0, 2f),
            0f,
            PhysicsBodyMotionType.Dynamic,
            FixedRotation: false,
            GravityScale: 1f));
        ball.Entity = Entity.Create(2, "Ball");
        ball.CreateBoxFixture(new PhysicsBoxFixtureDef(0.5f, 0.5f, Vector2.Zero, 1f, 0.3f, 0.7f, false));

        for (var i = 0; i < 300; i++)
            world.Step(1f / 60f, 6, 2);

        contactCount.ShouldBeGreaterThan(0);
    }

    private sealed class RecordingContactListener(Action onContact) : IPhysicsContactListener
    {
        public void OnContactBegin(IPhysicsBody2D bodyA, IPhysicsBody2D bodyB, bool isTrigger) => onContact();
        public void OnContactEnd(IPhysicsBody2D bodyA, IPhysicsBody2D bodyB, bool isTrigger) { }
    }

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

    [Fact]
    public void Raycast_Miss_WhenNothingInPath()
    {
        using var world = new Box2DPhysicsWorld2D(Vector2.Zero);

        world.Raycast(Vector2.Zero, Vector2.UnitX, 10f).ShouldBeNull();
    }

    [Fact]
    public void Raycast_ReturnsClosestHit()
    {
        using var world = new Box2DPhysicsWorld2D(Vector2.Zero);
        var near = Entity.Create(1, "Near");
        var far = Entity.Create(2, "Far");
        CreateStaticBox(world, near, new Vector2(2f, 0f));
        CreateStaticBox(world, far, new Vector2(5f, 0f));

        var hit = world.Raycast(Vector2.Zero, Vector2.UnitX, 10f);

        hit.ShouldNotBeNull();
        hit.Value.Entity.Id.ShouldBe(near.Id);
        hit.Value.Distance.ShouldBeGreaterThan(0.9f);
        hit.Value.Distance.ShouldBeLessThan(2.1f);
    }

    [Fact]
    public void Raycast_IgnoresSpecifiedEntity()
    {
        using var world = new Box2DPhysicsWorld2D(Vector2.Zero);
        var self = Entity.Create(1, "Self");
        var target = Entity.Create(2, "Target");
        CreateStaticBox(world, self, Vector2.Zero);
        CreateStaticBox(world, target, new Vector2(3f, 0f));

        var hit = world.Raycast(Vector2.Zero, Vector2.UnitX, 10f, self);

        hit.ShouldNotBeNull();
        hit.Value.Entity.Id.ShouldBe(target.Id);
    }

    [Fact]
    public void Raycast_SkipsTriggersByDefault()
    {
        using var world = new Box2DPhysicsWorld2D(Vector2.Zero);
        var trigger = Entity.Create(1, "Trigger");
        CreateStaticBox(world, trigger, new Vector2(2f, 0f), isTrigger: true);

        world.Raycast(Vector2.Zero, Vector2.UnitX, 10f).ShouldBeNull();
    }

    [Fact]
    public void Raycast_HitsTriggersWhenRequested()
    {
        using var world = new Box2DPhysicsWorld2D(Vector2.Zero);
        var trigger = Entity.Create(1, "Trigger");
        CreateStaticBox(world, trigger, new Vector2(2f, 0f), isTrigger: true);

        var hit = world.Raycast(Vector2.Zero, Vector2.UnitX, 10f, includeTriggers: true);

        hit.ShouldNotBeNull();
        hit.Value.Entity.Id.ShouldBe(trigger.Id);
        hit.Value.IsTrigger.ShouldBeTrue();
    }

    [Fact]
    public void Raycast_InvalidDistance_ReturnsNull()
    {
        using var world = new Box2DPhysicsWorld2D(Vector2.Zero);
        var wall = Entity.Create(1, "Wall");
        CreateStaticBox(world, wall, new Vector2(2f, 0f));

        world.Raycast(Vector2.Zero, Vector2.UnitX, 0f).ShouldBeNull();
        world.Raycast(Vector2.Zero, Vector2.UnitX, -1f).ShouldBeNull();
    }

    [Fact]
    public void OverlapCircle_Miss_WhenNothingOverlaps()
    {
        using var world = new Box2DPhysicsWorld2D(Vector2.Zero);

        world.OverlapCircle(new Vector2(10f, 10f), 1f).ShouldBeNull();
    }

    [Fact]
    public void OverlapCircle_ReturnsHit()
    {
        using var world = new Box2DPhysicsWorld2D(Vector2.Zero);
        var target = Entity.Create(1, "Target");
        CreateStaticBox(world, target, new Vector2(1f, 0f));

        var hit = world.OverlapCircle(Vector2.Zero, 2f);

        hit.ShouldNotBeNull();
        hit.Value.Entity.Id.ShouldBe(target.Id);
        hit.Value.Point.ShouldBe(Vector2.Zero);
        hit.Value.Distance.ShouldBe(0f);
    }

    [Fact]
    public void OverlapCircle_IgnoresSpecifiedEntity()
    {
        using var world = new Box2DPhysicsWorld2D(Vector2.Zero);
        var self = Entity.Create(1, "Self");
        var target = Entity.Create(2, "Target");
        CreateStaticBox(world, self, Vector2.Zero);
        CreateStaticBox(world, target, new Vector2(3f, 0f));

        var hit = world.OverlapCircle(Vector2.Zero, 5f, self);

        hit.ShouldNotBeNull();
        hit.Value.Entity.Id.ShouldBe(target.Id);
    }

    [Fact]
    public void OverlapCircle_SkipsTriggersByDefault()
    {
        using var world = new Box2DPhysicsWorld2D(Vector2.Zero);
        var trigger = Entity.Create(1, "Trigger");
        CreateStaticBox(world, trigger, Vector2.Zero, isTrigger: true);

        world.OverlapCircle(Vector2.Zero, 2f).ShouldBeNull();
    }

    [Fact]
    public void OverlapCircle_HitsTriggersWhenRequested()
    {
        using var world = new Box2DPhysicsWorld2D(Vector2.Zero);
        var trigger = Entity.Create(1, "Trigger");
        CreateStaticBox(world, trigger, Vector2.Zero, isTrigger: true);

        var hit = world.OverlapCircle(Vector2.Zero, 2f, includeTriggers: true);

        hit.ShouldNotBeNull();
        hit.Value.Entity.Id.ShouldBe(trigger.Id);
        hit.Value.IsTrigger.ShouldBeTrue();
    }

    [Fact]
    public void OverlapCircle_InvalidRadius_ReturnsNull()
    {
        using var world = new Box2DPhysicsWorld2D(Vector2.Zero);
        var wall = Entity.Create(1, "Wall");
        CreateStaticBox(world, wall, Vector2.Zero);

        world.OverlapCircle(Vector2.Zero, 0f).ShouldBeNull();
        world.OverlapCircle(Vector2.Zero, -1f).ShouldBeNull();
    }

    [Fact]
    public void CreateCircleFixture_CreatesFixtureAndParticipatesInOverlap()
    {
        using var world = new Box2DPhysicsWorld2D(Vector2.Zero);
        var target = Entity.Create(1, "Circle");
        var body = world.CreateBody(new PhysicsBodyDef(
            Vector2.Zero, 0f, PhysicsBodyMotionType.Static, FixedRotation: false, GravityScale: 0f));
        body.Entity = target;
        body.CreateCircleFixture(new PhysicsCircleFixtureDef(1f, Vector2.Zero, 0f, 0.5f, 0f, false));

        body.HasFixture.ShouldBeTrue();
        var hit = world.OverlapCircle(Vector2.Zero, 2f);
        hit.ShouldNotBeNull();
        hit.Value.Entity.Id.ShouldBe(target.Id);
    }

    [Fact]
    public void CreateCircleFixture_InvalidRadius_DoesNotCreateFixture()
    {
        using var world = new Box2DPhysicsWorld2D(Vector2.Zero);
        var body = world.CreateBody(new PhysicsBodyDef(
            Vector2.Zero, 0f, PhysicsBodyMotionType.Static, FixedRotation: false, GravityScale: 0f));

        body.CreateCircleFixture(new PhysicsCircleFixtureDef(0f, Vector2.Zero, 0f, 0.5f, 0f, false));
        body.HasFixture.ShouldBeFalse();
    }

    [Fact]
    public void CreateEdgeFixture_CreatesFixtureForSegment()
    {
        using var world = new Box2DPhysicsWorld2D(Vector2.Zero);
        var body = world.CreateBody(new PhysicsBodyDef(
            Vector2.Zero, 0f, PhysicsBodyMotionType.Static, FixedRotation: false, GravityScale: 0f));

        body.CreateEdgeFixture(new PhysicsEdgeFixtureDef(
            [new Vector2(-2f, 0f), new Vector2(2f, 0f)], 0f, 0.5f, 0f, false));

        body.HasFixture.ShouldBeTrue();
    }

    [Fact]
    public void CreateEdgeFixture_CreatesFixtureForChain()
    {
        using var world = new Box2DPhysicsWorld2D(Vector2.Zero);
        var body = world.CreateBody(new PhysicsBodyDef(
            Vector2.Zero, 0f, PhysicsBodyMotionType.Static, FixedRotation: false, GravityScale: 0f));

        body.CreateEdgeFixture(new PhysicsEdgeFixtureDef(
            [new Vector2(-2f, 0f), new Vector2(0f, 1f), new Vector2(2f, 0f)], 0f, 0.5f, 0f, false));

        body.HasFixture.ShouldBeTrue();
    }

    [Fact]
    public void CreateEdgeFixture_TooFewPoints_DoesNotCreateFixture()
    {
        using var world = new Box2DPhysicsWorld2D(Vector2.Zero);
        var body = world.CreateBody(new PhysicsBodyDef(
            Vector2.Zero, 0f, PhysicsBodyMotionType.Static, FixedRotation: false, GravityScale: 0f));

        body.CreateEdgeFixture(new PhysicsEdgeFixtureDef([Vector2.Zero], 0f, 0.5f, 0f, false));
        body.HasFixture.ShouldBeFalse();
    }

    private static IPhysicsBody2D CreateStaticBox(
        Box2DPhysicsWorld2D world,
        Entity entity,
        Vector2 position,
        float halfWidth = 1f,
        float halfHeight = 1f,
        bool isTrigger = false)
    {
        var body = world.CreateBody(new PhysicsBodyDef(
            position,
            0f,
            PhysicsBodyMotionType.Static,
            FixedRotation: false,
            GravityScale: 0f));
        body.Entity = entity;
        body.CreateBoxFixture(new PhysicsBoxFixtureDef(halfWidth, halfHeight, Vector2.Zero, 0f, 0.5f, 0f, isTrigger));
        return body;
    }
}
