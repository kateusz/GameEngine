using System.Numerics;
using ECS;
using Engine.Physics;
using Engine.Platform.Bepu;
using Engine.Scene.Systems;
using NSubstitute;
using SceneComponents;
using SceneComponents.Physics;
using Shouldly;

namespace Engine.Tests.Systems;

public class PhysicsSimulationSystem3DTests
{
    [Fact]
    public void Priority_ShouldReturnSystemPriority()
    {
        var (system, _, _, _) = CreateFullSystem();
        system.Priority.ShouldBe(SystemPriorities.PhysicsSimulationSystem);
    }

    [Fact]
    public void OnInit_WithRigidBodyAndCollider_CreatesBodyInStore()
    {
        var (system, context, world, bodyStore) = CreateFullSystem();
        var entity = CreateEntityWithBox(context);
        var mockBody = Substitute.For<IPhysicsBody3D>();
        world.CreateBody(Arg.Any<PhysicsBodyDef3D>()).Returns(mockBody);

        system.OnInit();

        bodyStore.TryGet(entity.Id, out var storedBody).ShouldBeTrue();
        storedBody.ShouldBe(mockBody);
        mockBody.Received(1).Entity = entity;
        mockBody.Received(1).CreateBoxFixture(Arg.Any<PhysicsBoxFixtureDef3D>());
    }

    [Fact]
    public void OnInit_WithoutCollider_DoesNotCreateBody()
    {
        var (system, context, world, bodyStore) = CreateFullSystem();
        CreateEntityWithTransformAndRb(context);
        world.CreateBody(Arg.Any<PhysicsBodyDef3D>()).Returns(Substitute.For<IPhysicsBody3D>());

        system.OnInit();

        world.DidNotReceive().CreateBody(Arg.Any<PhysicsBodyDef3D>());
        bodyStore.Snapshot().ShouldBeEmpty();
    }

    [Fact]
    public void OnUpdate_StepsPhysicsWorldOncePerTimestep()
    {
        var (system, _, world, _) = CreateFullSystem();
        system.OnUpdate(TimeSpan.FromSeconds(0.017));
        world.Received(1).Step(PhysicsConstants.PhysicsTimestep, 6, 2);
    }

    [Fact]
    public void OnUpdate_SyncsBodyPoseToTransform()
    {
        var (system, context, world, _) = CreateFullSystem();
        var entity = CreateEntityWithBox(context);
        var transform = entity.GetComponent<TransformComponent>();

        var mockBody = Substitute.For<IPhysicsBody3D>();
        mockBody.Position.Returns(new Vector3(5, 10, 2));
        mockBody.Orientation.Returns(Quaternion.Identity);
        mockBody.LinearVelocity.Returns(new Vector3(2, 3, 1));
        world.CreateBody(Arg.Any<PhysicsBodyDef3D>()).Returns(mockBody);

        system.OnInit();
        system.OnUpdate(TimeSpan.FromSeconds(0.017));

        transform.Translation.ShouldBe(new Vector3(5, 10, 2));
    }

    [Fact]
    public void OnInit_FixedRotation_CreatesBodyWithIdentityOrientation()
    {
        var (system, context, world, _) = CreateFullSystem();
        var entity = CreateEntityWithBox(context);
        entity.GetComponent<TransformComponent>().Rotation = new Vector3(-MathF.PI / 2f, 0, 0);
        entity.GetComponent<RigidBody3DComponent>().FixedRotation = true;
        world.CreateBody(Arg.Any<PhysicsBodyDef3D>()).Returns(Substitute.For<IPhysicsBody3D>());

        system.OnInit();

        world.Received(1).CreateBody(Arg.Is<PhysicsBodyDef3D>(d =>
            d.Orientation == Quaternion.Identity && d.FixedRotation));
    }

    [Fact]
    public void OnUpdate_FixedRotation_DoesNotOverwriteTransformRotation()
    {
        var (system, context, world, _) = CreateFullSystem();
        var entity = CreateEntityWithBox(context);
        var transform = entity.GetComponent<TransformComponent>();
        var mixamoLie = new Vector3(-MathF.PI / 2f, 0.3f, 0);
        transform.Rotation = mixamoLie;
        entity.GetComponent<RigidBody3DComponent>().FixedRotation = true;

        var mockBody = Substitute.For<IPhysicsBody3D>();
        mockBody.Position.Returns(new Vector3(1, 2, 3));
        mockBody.Orientation.Returns(Quaternion.Identity);
        world.CreateBody(Arg.Any<PhysicsBodyDef3D>()).Returns(mockBody);

        system.OnInit();
        system.OnUpdate(TimeSpan.FromSeconds(0.017));

        transform.Translation.ShouldBe(new Vector3(1, 2, 3));
        transform.Rotation.ShouldBe(mixamoLie);
    }

    [Fact]
    public void OnUpdate_PushesComponentVelocityToBodyForDynamicBodies()
    {
        var (system, context, world, _) = CreateFullSystem();
        var entity = CreateEntityWithBox(context);
        var rb = entity.GetComponent<RigidBody3DComponent>();
        rb.BodyType = RigidBodyType.Dynamic;
        rb.Velocity = new Vector3(10, 20, 5);

        var mockBody = Substitute.For<IPhysicsBody3D>();
        mockBody.Position.Returns(Vector3.Zero);
        mockBody.Orientation.Returns(Quaternion.Identity);
        world.CreateBody(Arg.Any<PhysicsBodyDef3D>()).Returns(mockBody);

        system.OnInit();
        system.OnUpdate(TimeSpan.FromSeconds(0.017));

        mockBody.Received(1).LinearVelocity = new Vector3(10, 20, 5);
    }

    [Fact]
    public void OnUpdate_CleansUpOrphanedBodies()
    {
        var (system, context, world, bodyStore) = CreateFullSystem();
        var entity = CreateEntityWithBox(context);
        var mockBody = Substitute.For<IPhysicsBody3D>();
        world.CreateBody(Arg.Any<PhysicsBodyDef3D>()).Returns(mockBody);

        system.OnInit();
        context.Remove(entity.Id);
        system.OnUpdate(TimeSpan.FromSeconds(0.017));

        world.Received(1).DestroyBody(mockBody);
        bodyStore.TryGet(entity.Id, out _).ShouldBeFalse();
    }

    [Fact]
    public void OnShutdown_DestroysAllBodiesAndClearsStore()
    {
        var (system, context, world, bodyStore) = CreateFullSystem();
        CreateEntityWithBox(context);
        var mockBody = Substitute.For<IPhysicsBody3D>();
        world.CreateBody(Arg.Any<PhysicsBodyDef3D>()).Returns(mockBody);

        system.OnInit();
        system.OnShutdown();

        world.Received(1).DestroyBody(mockBody);
        bodyStore.Snapshot().ShouldBeEmpty();
    }

    [Fact]
    public void Dispose_DisposesPhysicsWorld()
    {
        var world = Substitute.For<IPhysicsWorld3D>();
        var system = new PhysicsSimulationSystem3D(world, new Context(), new PhysicsRuntimeBodyStore3D());

        system.Dispose();

        world.Received(1).Dispose();
    }

    [Fact]
    public void OnInit_PrefersBoxWhenMultipleCollidersPresent()
    {
        var (system, context, world, _) = CreateFullSystem();
        var entity = CreateEntityWithTransformAndRb(context);
        entity.AddComponent(new BoxCollider3DComponent());
        entity.AddComponent(new SphereCollider3DComponent());
        entity.AddComponent(new CapsuleCollider3DComponent());
        var mockBody = Substitute.For<IPhysicsBody3D>();
        world.CreateBody(Arg.Any<PhysicsBodyDef3D>()).Returns(mockBody);

        system.OnInit();

        mockBody.Received(1).CreateBoxFixture(Arg.Any<PhysicsBoxFixtureDef3D>());
        mockBody.DidNotReceive().CreateSphereFixture(Arg.Any<PhysicsSphereFixtureDef3D>());
        mockBody.DidNotReceive().CreateCapsuleFixture(Arg.Any<PhysicsCapsuleFixtureDef3D>());
    }

    [Fact]
    public void OnInit_WithSphereCollider_CreatesSphereFixture()
    {
        var (system, context, world, _) = CreateFullSystem();
        var entity = CreateEntityWithTransformAndRb(context);
        entity.AddComponent(new SphereCollider3DComponent { Radius = 1.25f });
        var mockBody = Substitute.For<IPhysicsBody3D>();
        world.CreateBody(Arg.Any<PhysicsBodyDef3D>()).Returns(mockBody);

        system.OnInit();

        mockBody.Received(1).CreateSphereFixture(Arg.Is<PhysicsSphereFixtureDef3D>(d =>
            d.Radius == 1.25f && !d.IsSensor));
    }

    [Fact]
    public void OnUpdate_UnchangedIdentity_DoesNotRecreateBody()
    {
        var (system, context, world, bodyStore) = CreateFullSystem();
        var entity = CreateEntityWithBox(context);
        var mockBody = StubBody();
        world.CreateBody(Arg.Any<PhysicsBodyDef3D>()).Returns(mockBody);

        system.OnInit();
        system.OnUpdate(TimeSpan.FromSeconds(0.017));

        world.Received(1).CreateBody(Arg.Any<PhysicsBodyDef3D>());
        world.DidNotReceive().DestroyBody(Arg.Any<IPhysicsBody3D>());
        bodyStore.TryGet(entity.Id, out var stored).ShouldBeTrue();
        stored.ShouldBe(mockBody);
    }

    [Fact]
    public void OnUpdate_BodyTypeChange_RecreatesBody()
    {
        var (system, context, world, bodyStore) = CreateFullSystem();
        var entity = CreateEntityWithBox(context);
        var (first, second) = ArrangeTwoBodies(world);

        system.OnInit();
        entity.GetComponent<RigidBody3DComponent>().BodyType = RigidBodyType.Kinematic;
        system.OnUpdate(TimeSpan.FromSeconds(0.017));

        world.Received(1).DestroyBody(first);
        world.Received().CreateBody(Arg.Is<PhysicsBodyDef3D>(d => d.MotionType == PhysicsBodyMotionType.Kinematic));
        bodyStore.TryGet(entity.Id, out var stored).ShouldBeTrue();
        stored.ShouldBe(second);
    }

    [Fact]
    public void OnUpdate_BoxSizeChange_RecreatesBody()
    {
        var (system, context, world, bodyStore) = CreateFullSystem();
        var entity = CreateEntityWithBox(context);
        var (first, second) = ArrangeTwoBodies(world);

        system.OnInit();
        entity.GetComponent<BoxCollider3DComponent>().Size = new Vector3(2f);
        system.OnUpdate(TimeSpan.FromSeconds(0.017));

        world.Received(1).DestroyBody(first);
        second.Received(1).CreateBoxFixture(Arg.Is<PhysicsBoxFixtureDef3D>(d => d.HalfExtents == new Vector3(2f)));
        bodyStore.TryGet(entity.Id, out var stored).ShouldBeTrue();
        stored.ShouldBe(second);
    }

    [Fact]
    public void OnUpdate_ScaleChange_RecreatesBody()
    {
        var (system, context, world, _) = CreateFullSystem();
        var entity = CreateEntityWithBox(context);
        var (first, second) = ArrangeTwoBodies(world);

        system.OnInit();
        entity.GetComponent<TransformComponent>().Scale = new Vector3(2f);
        system.OnUpdate(TimeSpan.FromSeconds(0.017));

        world.Received(1).DestroyBody(first);
        second.Received(1).CreateBoxFixture(Arg.Is<PhysicsBoxFixtureDef3D>(d => d.HalfExtents == new Vector3(1f)));
    }

    [Fact]
    public void OnUpdate_GravityScaleChange_RecreatesBody()
    {
        var (system, context, world, _) = CreateFullSystem();
        var entity = CreateEntityWithBox(context);
        ArrangeTwoBodies(world);

        system.OnInit();
        entity.GetComponent<RigidBody3DComponent>().GravityScale = 0f;
        system.OnUpdate(TimeSpan.FromSeconds(0.017));

        world.Received().CreateBody(Arg.Is<PhysicsBodyDef3D>(d => d.GravityScale == 0f));
        world.Received(1).DestroyBody(Arg.Any<IPhysicsBody3D>());
    }

    [Fact]
    public void OnUpdate_FixedRotationChange_RecreatesBody()
    {
        var (system, context, world, _) = CreateFullSystem();
        var entity = CreateEntityWithBox(context);
        ArrangeTwoBodies(world);

        system.OnInit();
        entity.GetComponent<RigidBody3DComponent>().FixedRotation = true;
        system.OnUpdate(TimeSpan.FromSeconds(0.017));

        world.Received().CreateBody(Arg.Is<PhysicsBodyDef3D>(d => d.FixedRotation));
        world.Received(1).DestroyBody(Arg.Any<IPhysicsBody3D>());
    }

    [Fact]
    public void OnUpdate_DensityChange_RecreatesBody()
    {
        var (system, context, world, _) = CreateFullSystem();
        var entity = CreateEntityWithBox(context);
        var (first, second) = ArrangeTwoBodies(world);

        system.OnInit();
        entity.GetComponent<BoxCollider3DComponent>().Density = 4f;
        system.OnUpdate(TimeSpan.FromSeconds(0.017));

        world.Received(1).DestroyBody(first);
        second.Received(1).CreateBoxFixture(Arg.Is<PhysicsBoxFixtureDef3D>(d => d.Density == 4f));
    }

    [Fact]
    public void OnUpdate_IsTriggerChange_RecreatesBody()
    {
        var (system, context, world, _) = CreateFullSystem();
        var entity = CreateEntityWithBox(context);
        var (first, second) = ArrangeTwoBodies(world);

        system.OnInit();
        entity.GetComponent<BoxCollider3DComponent>().IsTrigger = true;
        system.OnUpdate(TimeSpan.FromSeconds(0.017));

        world.Received(1).DestroyBody(first);
        second.Received(1).CreateBoxFixture(Arg.Is<PhysicsBoxFixtureDef3D>(d => d.IsSensor));
    }

    [Fact]
    public void OnUpdate_BoxToSphere_RecreatesWithSphereFixture()
    {
        var (system, context, world, bodyStore) = CreateFullSystem();
        var entity = CreateEntityWithBox(context);
        var (first, second) = ArrangeTwoBodies(world);

        system.OnInit();
        entity.RemoveComponent<BoxCollider3DComponent>();
        entity.AddComponent(new SphereCollider3DComponent { Radius = 1.25f });
        system.OnUpdate(TimeSpan.FromSeconds(0.017));

        world.Received(1).DestroyBody(first);
        first.DidNotReceive().CreateSphereFixture(Arg.Any<PhysicsSphereFixtureDef3D>());
        second.Received(1).CreateSphereFixture(Arg.Is<PhysicsSphereFixtureDef3D>(d => d.Radius == 1.25f));
        bodyStore.TryGet(entity.Id, out var stored).ShouldBeTrue();
        stored.ShouldBe(second);
    }

    [Fact]
    public void OnUpdate_ColliderRemoved_DropsBodyFromStore()
    {
        var (system, context, world, bodyStore) = CreateFullSystem();
        var entity = CreateEntityWithBox(context);
        var (first, _) = ArrangeTwoBodies(world);

        system.OnInit();
        entity.RemoveComponent<BoxCollider3DComponent>();
        system.OnUpdate(TimeSpan.FromSeconds(0.017));

        world.Received(1).DestroyBody(first);
        world.Received(1).CreateBody(Arg.Any<PhysicsBodyDef3D>());
        bodyStore.TryGet(entity.Id, out _).ShouldBeFalse();
    }

    [Fact]
    public void RealWorld_DropSettlesOnFloor()
    {
        using var world = new BepuPhysicsWorld3D(new Vector3(0, -9.8f, 0));
        var context = new Context();
        var bodyStore = new PhysicsRuntimeBodyStore3D();
        var system = new PhysicsSimulationSystem3D(world, context, bodyStore);

        var floor = Entity.Create(1, "Floor");
        floor.AddComponent(new TransformComponent());
        floor.AddComponent(new RigidBody3DComponent { BodyType = RigidBodyType.Static });
        floor.AddComponent(new BoxCollider3DComponent { Size = new Vector3(10f, 0.5f, 10f) });
        context.Register(floor);

        var box = Entity.Create(2, "Box");
        box.AddComponent(new TransformComponent { Translation = new Vector3(0, 5f, 0) });
        box.AddComponent(new RigidBody3DComponent { BodyType = RigidBodyType.Dynamic, GravityScale = 1f });
        box.AddComponent(new BoxCollider3DComponent { Size = new Vector3(0.5f) });
        context.Register(box);

        system.OnInit();
        for (var i = 0; i < 120; i++)
            system.OnUpdate(TimeSpan.FromSeconds(PhysicsConstants.PhysicsTimestep));

        box.GetComponent<TransformComponent>().Translation.Y.ShouldBe(1f, 0.15);
    }

    [Fact]
    public void RealWorld_BoxColliderOffset_ShiftsRestPosition()
    {
        using var world = new BepuPhysicsWorld3D(new Vector3(0, -9.8f, 0));
        var context = new Context();
        var bodyStore = new PhysicsRuntimeBodyStore3D();
        var system = new PhysicsSimulationSystem3D(world, context, bodyStore);

        var floor = Entity.Create(1, "Floor");
        floor.AddComponent(new TransformComponent());
        floor.AddComponent(new RigidBody3DComponent { BodyType = RigidBodyType.Static });
        floor.AddComponent(new BoxCollider3DComponent { Size = new Vector3(10f, 0.5f, 10f) });
        context.Register(floor);

        var box = Entity.Create(2, "Box");
        box.AddComponent(new TransformComponent { Translation = new Vector3(0, 5f, 0), Scale = Vector3.One });
        box.AddComponent(new RigidBody3DComponent { BodyType = RigidBodyType.Dynamic, GravityScale = 1f });
        box.AddComponent(new BoxCollider3DComponent { Size = new Vector3(0.5f), Offset = new Vector3(0f, -1f, 0f) });
        context.Register(box);

        system.OnInit();
        for (var i = 0; i < 180; i++)
            system.OnUpdate(TimeSpan.FromSeconds(PhysicsConstants.PhysicsTimestep));

        box.GetComponent<TransformComponent>().Translation.Y.ShouldBe(2f, 0.2);
    }

    [Fact]
    public void RealWorld_GravityScaleChange_StopsAccelerating()
    {
        using var world = new BepuPhysicsWorld3D(new Vector3(0, -9.8f, 0));
        var context = new Context();
        var bodyStore = new PhysicsRuntimeBodyStore3D();
        var system = new PhysicsSimulationSystem3D(world, context, bodyStore);

        var box = Entity.Create(1, "Box");
        box.AddComponent(new TransformComponent { Translation = new Vector3(0, 10f, 0) });
        box.AddComponent(new RigidBody3DComponent { BodyType = RigidBodyType.Dynamic, GravityScale = 1f });
        box.AddComponent(new BoxCollider3DComponent { Size = new Vector3(0.5f) });
        context.Register(box);

        system.OnInit();
        for (var i = 0; i < 20; i++)
            system.OnUpdate(TimeSpan.FromSeconds(PhysicsConstants.PhysicsTimestep));

        var vyMid = box.GetComponent<RigidBody3DComponent>().Velocity.Y;
        vyMid.ShouldBeLessThan(-1f);

        box.GetComponent<RigidBody3DComponent>().GravityScale = 0f;
        for (var i = 0; i < 20; i++)
            system.OnUpdate(TimeSpan.FromSeconds(PhysicsConstants.PhysicsTimestep));

        box.GetComponent<RigidBody3DComponent>().Velocity.Y.ShouldBe(vyMid, 0.15);
    }

    private static (PhysicsSimulationSystem3D System, IContext Context, IPhysicsWorld3D World, PhysicsRuntimeBodyStore3D BodyStore)
        CreateFullSystem()
    {
        var world = Substitute.For<IPhysicsWorld3D>();
        var context = new Context();
        var bodyStore = new PhysicsRuntimeBodyStore3D();
        return (new PhysicsSimulationSystem3D(world, context, bodyStore), context, world, bodyStore);
    }

    private static Entity CreateEntityWithTransformAndRb(IContext context)
    {
        var entity = Entity.Create(1, "test");
        entity.AddComponent<RigidBody3DComponent>();
        entity.AddComponent<TransformComponent>();
        context.Register(entity);
        return entity;
    }

    private static Entity CreateEntityWithBox(IContext context)
    {
        var entity = CreateEntityWithTransformAndRb(context);
        entity.AddComponent<BoxCollider3DComponent>();
        return entity;
    }

    private static (IPhysicsBody3D First, IPhysicsBody3D Second) ArrangeTwoBodies(IPhysicsWorld3D world)
    {
        var first = StubBody();
        var second = StubBody();
        world.CreateBody(Arg.Any<PhysicsBodyDef3D>()).Returns(first, second);
        return (first, second);
    }

    private static IPhysicsBody3D StubBody()
    {
        var body = Substitute.For<IPhysicsBody3D>();
        body.Position.Returns(Vector3.Zero);
        body.Orientation.Returns(Quaternion.Identity);
        body.LinearVelocity.Returns(Vector3.Zero);
        return body;
    }
}
