using System.Numerics;
using ECS;
using Engine.Physics;
using Engine.Platform.Bepu;
using Shouldly;

namespace Engine.Tests.Physics;

public class BepuPhysicsWorld3DTests
{
    [Fact]
    public void BoxOffset_ShiftsCollisionShapeRelativeToBodyPosition()
    {
        using var world = new BepuPhysicsWorld3D(new Vector3(0, -9.8f, 0));
        var floor = world.CreateBody(FloorDef());
        floor.Entity = Entity.Create(1, "Floor");
        floor.CreateBoxFixture(new PhysicsBoxFixtureDef3D(new Vector3(10f, 0.5f, 10f), Vector3.Zero, 0f, 0.5f, 0f, false));

        var box = world.CreateBody(new PhysicsBodyDef3D(
            new Vector3(0, 5f, 0), Quaternion.Identity, PhysicsBodyMotionType.Dynamic, false, 1f));
        box.Entity = Entity.Create(2, "Box");
        box.CreateBoxFixture(new PhysicsBoxFixtureDef3D(
            new Vector3(0.5f), new Vector3(0f, -1f, 0f), 1f, 0.5f, 0f, false));

        for (var i = 0; i < 180; i++)
            world.Step(PhysicsConstants.PhysicsTimestep, 6, 2);

        // Collider sits at Y=1; offset -1 keeps the body origin at Y=2.
        box.Position.Y.ShouldBe(2f, 0.2);
    }

    [Fact]
    public void Factory_ThreeD_CreatesBepuWorldAndDropSettles()
    {
        IPhysicsWorld2DFactory factory = new PhysicsWorld2DFactory(new PhysicsBackendConfig(PhysicsBackendType.Box2D));
        using var world = factory.Create3D(new Vector3(0, -9.8f, 0));

        var floor = world.CreateBody(FloorDef());
        floor.Entity = Entity.Create(1, "Floor");
        floor.CreateBoxFixture(new PhysicsBoxFixtureDef3D(new Vector3(10f, 0.5f, 10f), Vector3.Zero, 0f, 0.5f, 0f, false));

        var box = world.CreateBody(new PhysicsBodyDef3D(
            new Vector3(0, 5f, 0),
            Quaternion.Identity,
            PhysicsBodyMotionType.Dynamic,
            FixedRotation: false,
            GravityScale: 1f));
        box.Entity = Entity.Create(2, "Box");
        box.CreateBoxFixture(new PhysicsBoxFixtureDef3D(new Vector3(0.5f), Vector3.Zero, 1f, 0.5f, 0f, false));

        for (var i = 0; i < 120; i++)
            world.Step(PhysicsConstants.PhysicsTimestep, 6, 2);

        box.Position.Y.ShouldBe(1f, 0.15);
    }

    [Fact]
    public void Dispose_Twice_DoesNotThrow()
    {
        var world = new BepuPhysicsWorld3D(new Vector3(0, -9.8f, 0));
        world.Dispose();
        world.Dispose();
    }

    [Fact]
    public void Step_ContactBegin_FiresWhenBoxLands()
    {
        using var world = new BepuPhysicsWorld3D(new Vector3(0, -20f, 0));
        var begins = 0;
        world.SetContactListener(new RecordingListener3D(onBegin: (_, _, isTrigger) =>
        {
            if (!isTrigger)
                begins++;
        }));

        var floor = world.CreateBody(FloorDef());
        floor.Entity = Entity.Create(1, "Floor");
        floor.CreateBoxFixture(new PhysicsBoxFixtureDef3D(new Vector3(10f, 0.5f, 10f), Vector3.Zero, 0f, 0.5f, 0f, false));

        var box = world.CreateBody(new PhysicsBodyDef3D(
            new Vector3(0, 4f, 0), Quaternion.Identity, PhysicsBodyMotionType.Dynamic, false, 1f));
        box.Entity = Entity.Create(2, "Box");
        box.CreateBoxFixture(new PhysicsBoxFixtureDef3D(new Vector3(0.5f), Vector3.Zero, 1f, 0.3f, 0f, false));

        for (var i = 0; i < 180; i++)
            world.Step(PhysicsConstants.PhysicsTimestep, 6, 2);

        begins.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Step_TriggerPair_FiresWithoutBlockingFall()
    {
        using var world = new BepuPhysicsWorld3D(new Vector3(0, -20f, 0));
        var triggerBegins = 0;
        world.SetContactListener(new RecordingListener3D(onBegin: (_, _, isTrigger) =>
        {
            if (isTrigger)
                triggerBegins++;
        }));

        var trigger = world.CreateBody(new PhysicsBodyDef3D(
            new Vector3(0, 2f, 0), Quaternion.Identity, PhysicsBodyMotionType.Static, false, 0f));
        trigger.Entity = Entity.Create(1, "Trigger");
        trigger.CreateBoxFixture(new PhysicsBoxFixtureDef3D(new Vector3(2f, 0.25f, 2f), Vector3.Zero, 0f, 0.5f, 0f, true));

        var box = world.CreateBody(new PhysicsBodyDef3D(
            new Vector3(0, 5f, 0), Quaternion.Identity, PhysicsBodyMotionType.Dynamic, false, 1f));
        box.Entity = Entity.Create(2, "Box");
        box.CreateBoxFixture(new PhysicsBoxFixtureDef3D(new Vector3(0.5f), Vector3.Zero, 1f, 0.3f, 0f, false));

        for (var i = 0; i < 180; i++)
            world.Step(PhysicsConstants.PhysicsTimestep, 6, 2);

        triggerBegins.ShouldBeGreaterThan(0);
        box.Position.Y.ShouldBeLessThan(1.5f);
    }

    [Fact]
    public void Raycast3D_HitsClosestBody()
    {
        using var world = new BepuPhysicsWorld3D(Vector3.Zero);
        var near = world.CreateBody(new PhysicsBodyDef3D(
            new Vector3(0, 0, 2f), Quaternion.Identity, PhysicsBodyMotionType.Static, false, 0f));
        near.Entity = Entity.Create(1, "Near");
        near.CreateBoxFixture(new PhysicsBoxFixtureDef3D(new Vector3(0.5f), Vector3.Zero, 0f, 0.5f, 0f, false));

        var far = world.CreateBody(new PhysicsBodyDef3D(
            new Vector3(0, 0, 6f), Quaternion.Identity, PhysicsBodyMotionType.Static, false, 0f));
        far.Entity = Entity.Create(2, "Far");
        far.CreateBoxFixture(new PhysicsBoxFixtureDef3D(new Vector3(0.5f), Vector3.Zero, 0f, 0.5f, 0f, false));

        var hit = world.Raycast(Vector3.Zero, Vector3.UnitZ, 20f);
        hit.ShouldNotBeNull();
        hit.Value.Entity.Id.ShouldBe(1);
        hit.Value.IsTrigger.ShouldBeFalse();
    }

    [Fact]
    public void Raycast3D_Miss_WhenNothingInPath()
    {
        using var world = new BepuPhysicsWorld3D(Vector3.Zero);
        world.Raycast(Vector3.Zero, Vector3.UnitY, 10f).ShouldBeNull();
    }

    [Fact]
    public void Raycast3D_IgnoreEntity_SkipsSelf()
    {
        using var world = new BepuPhysicsWorld3D(Vector3.Zero);
        var self = Entity.Create(1, "Self");
        var body = world.CreateBody(new PhysicsBodyDef3D(
            new Vector3(0, 0, 2f), Quaternion.Identity, PhysicsBodyMotionType.Static, false, 0f));
        body.Entity = self;
        body.CreateBoxFixture(new PhysicsBoxFixtureDef3D(new Vector3(0.5f), Vector3.Zero, 0f, 0.5f, 0f, false));

        world.Raycast(Vector3.Zero, Vector3.UnitZ, 20f, self).ShouldBeNull();
    }

    [Fact]
    public void Raycast3D_SkipsTriggers_UnlessIncluded()
    {
        using var world = new BepuPhysicsWorld3D(Vector3.Zero);
        var body = world.CreateBody(new PhysicsBodyDef3D(
            new Vector3(0, 0, 2f), Quaternion.Identity, PhysicsBodyMotionType.Static, false, 0f));
        body.Entity = Entity.Create(1, "Trigger");
        body.CreateBoxFixture(new PhysicsBoxFixtureDef3D(new Vector3(0.5f), Vector3.Zero, 0f, 0.5f, 0f, true));

        world.Raycast(Vector3.Zero, Vector3.UnitZ, 20f).ShouldBeNull();
        var hit = world.Raycast(Vector3.Zero, Vector3.UnitZ, 20f, includeTriggers: true);
        hit.ShouldNotBeNull();
        hit.Value.IsTrigger.ShouldBeTrue();
    }

    [Fact]
    public void OverlapSphere_FindsNearbyBody()
    {
        using var world = new BepuPhysicsWorld3D(Vector3.Zero);
        var body = world.CreateBody(new PhysicsBodyDef3D(
            new Vector3(1f, 0, 0), Quaternion.Identity, PhysicsBodyMotionType.Static, false, 0f));
        body.Entity = Entity.Create(1, "Box");
        body.CreateBoxFixture(new PhysicsBoxFixtureDef3D(new Vector3(0.5f), Vector3.Zero, 0f, 0.5f, 0f, false));

        var hit = world.OverlapSphere(Vector3.Zero, 2f);
        hit.ShouldNotBeNull();
        hit.Value.Entity.Id.ShouldBe(1);
    }

    [Fact]
    public void ArenaFloor_BoxFromRest_SettlesOnThinSlab()
    {
        using var world = new BepuPhysicsWorld3D(new Vector3(0, -9.8f, 0));
        var floor = world.CreateBody(FloorDef());
        floor.Entity = Entity.Create(1, "Floor");
        floor.CreateBoxFixture(new PhysicsBoxFixtureDef3D(new Vector3(50f, 0.05f, 50f), Vector3.Zero, 1f, 0.5f, 0f, false));

        var box = world.CreateBody(new PhysicsBodyDef3D(
            new Vector3(-1.83f, 0.9f, -5.57f), Quaternion.Identity, PhysicsBodyMotionType.Dynamic, false, 1f));
        box.Entity = Entity.Create(2, "Box");
        box.CreateBoxFixture(new PhysicsBoxFixtureDef3D(new Vector3(0.15f), Vector3.Zero, 1f, 0.5f, 0.7f, false));

        for (var i = 0; i < 180; i++)
            world.Step(PhysicsConstants.PhysicsTimestep, 6, 2);

        box.Position.Y.ShouldBe(0.2f, 0.15);
    }

    [Fact]
    public void ArenaFloor_BoxWithPlayModeVelocity_DoesNotTunnel()
    {
        using var world = new BepuPhysicsWorld3D(new Vector3(0, -9.8f, 0));
        var floor = world.CreateBody(FloorDef());
        floor.Entity = Entity.Create(1, "Floor");
        floor.CreateBoxFixture(new PhysicsBoxFixtureDef3D(new Vector3(50f, 0.05f, 50f), Vector3.Zero, 1f, 0.5f, 0f, false));

        var box = world.CreateBody(new PhysicsBodyDef3D(
            new Vector3(-1.83f, 0.9f, -5.57f), Quaternion.Identity, PhysicsBodyMotionType.Dynamic, false, 1f));
        box.Entity = Entity.Create(2, "Box");
        box.CreateBoxFixture(new PhysicsBoxFixtureDef3D(new Vector3(0.15f), Vector3.Zero, 1f, 0.5f, 0.7f, false));
        box.LinearVelocity = new Vector3(0f, -92f, 0f);

        for (var i = 0; i < 180; i++)
            world.Step(PhysicsConstants.PhysicsTimestep, 6, 2);

        box.Position.Y.ShouldBeGreaterThan(0f);
    }

    [Fact]
    public void GravityScale_Zero_DoesNotFall()
    {
        using var world = new BepuPhysicsWorld3D(new Vector3(0, -9.8f, 0));
        var box = world.CreateBody(new PhysicsBodyDef3D(
            new Vector3(0, 5f, 0), Quaternion.Identity, PhysicsBodyMotionType.Dynamic, false, 0f));
        box.Entity = Entity.Create(1, "Box");
        box.CreateBoxFixture(new PhysicsBoxFixtureDef3D(new Vector3(0.5f), Vector3.Zero, 1f, 0.5f, 0f, false));

        for (var i = 0; i < 30; i++)
            world.Step(PhysicsConstants.PhysicsTimestep, 6, 2);

        box.Position.Y.ShouldBe(5f, 0.05);
    }

    private static PhysicsBodyDef3D FloorDef() =>
        new(new Vector3(0, 0, 0), Quaternion.Identity, PhysicsBodyMotionType.Static, false, 0f);

    private sealed class RecordingListener3D(Action<IPhysicsBody3D, IPhysicsBody3D, bool> onBegin) : IPhysicsContactListener3D, IPhysicsContactListener
    {
        public void OnContactBegin(IPhysicsBody3D bodyA, IPhysicsBody3D bodyB, bool isTrigger) =>
            onBegin(bodyA, bodyB, isTrigger);

        public void OnContactEnd(IPhysicsBody3D bodyA, IPhysicsBody3D bodyB, bool isTrigger) { }

        public void OnContactBegin(IPhysicsBody2D bodyA, IPhysicsBody2D bodyB, bool isTrigger) { }

        public void OnContactEnd(IPhysicsBody2D bodyA, IPhysicsBody2D bodyB, bool isTrigger) { }
    }
}
