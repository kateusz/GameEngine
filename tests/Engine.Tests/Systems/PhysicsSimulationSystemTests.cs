using System.Numerics;
using ECS;
using Engine.Physics;
using Engine.Scene.Systems;
using NSubstitute;
using SceneComponents;
using SceneComponents.Physics;
using Shouldly;

namespace Engine.Tests.Systems;

public class PhysicsSimulationSystemTests
{
    [Fact]
    public void Priority_ShouldReturnSystemPriority()
    {
        var (system, _, _, _) = CreateFullSystem();
        system.Priority.ShouldBe(SystemPriorities.PhysicsSimulationSystem);
    }

    [Fact]
    public void OnInit_WithRigidBodyAndTransform_CreatesBodyInStore()
    {
        var (system, context, world, bodyStore) = CreateFullSystem();
        var entity = CreateEntityWithTransformAndRb(context);
        var mockBody = Substitute.For<IPhysicsBody2D>();
        world.CreateBody(Arg.Any<PhysicsBodyDef>()).Returns(mockBody);

        system.OnInit();

        bodyStore.TryGet(entity.Id, out var storedBody).ShouldBeTrue();
        storedBody.ShouldBe(mockBody);
        mockBody.Received(1).Entity = entity;
    }

    [Fact]
    public void OnUpdate_StepsPhysicsWorldOncePerTimestep()
    {
        var (system, _, world, _) = CreateFullSystem();
        system.OnUpdate(TimeSpan.FromSeconds(0.017));
        world.Received(1).Step(PhysicsConstants.PhysicsTimestep, 6, 2);
    }

    [Fact]
    public void OnUpdate_AccumulatesTimeAndStepsMultipleTimes()
    {
        var (system, _, world, _) = CreateFullSystem();
        system.OnUpdate(TimeSpan.FromSeconds(0.051));
        world.Received(3).Step(PhysicsConstants.PhysicsTimestep, 6, 2);
    }

    [Fact]
    public void OnUpdate_SyncsBodyPositionAndRotationToTransform()
    {
        var (system, context, world, bodyStore) = CreateFullSystem();
        var entity = CreateEntityWithFullCollider(context);
        var transform = entity.GetComponent<TransformComponent>();

        var mockBody = Substitute.For<IPhysicsBody2D>();
        mockBody.Position.Returns(new Vector2(5, 10));
        mockBody.Angle.Returns(1.5f);
        mockBody.LinearVelocity.Returns(new Vector2(2, 3));
        world.CreateBody(Arg.Any<PhysicsBodyDef>()).Returns(mockBody);

        system.OnInit();
        system.OnUpdate(TimeSpan.FromSeconds(0.017));

        transform.Translation.X.ShouldBe(5);
        transform.Translation.Y.ShouldBe(10);
        transform.Rotation.Z.ShouldBe(1.5f);
    }

    [Fact]
    public void OnUpdate_PushesComponentVelocityToBodyForDynamicBodies()
    {
        var (system, context, world, bodyStore) = CreateFullSystem();
        var entity = CreateEntityWithFullCollider(context);
        var rb = entity.GetComponent<RigidBody2DComponent>();
        rb.BodyType = RigidBodyType.Dynamic;
        rb.Velocity = new Vector2(10, 20);

        var mockBody = Substitute.For<IPhysicsBody2D>();
        mockBody.Position.Returns(Vector2.Zero);
        mockBody.Angle.Returns(0f);
        world.CreateBody(Arg.Any<PhysicsBodyDef>()).Returns(mockBody);

        system.OnInit();
        system.OnUpdate(TimeSpan.FromSeconds(0.017));

        mockBody.Received(1).LinearVelocity = new Vector2(10, 20);
    }

    [Fact]
    public void OnUpdate_CleansUpOrphanedBodies()
    {
        var (system, context, world, bodyStore) = CreateFullSystem();
        var entity = CreateEntityWithTransformAndRb(context);
        var mockBody = Substitute.For<IPhysicsBody2D>();
        world.CreateBody(Arg.Any<PhysicsBodyDef>()).Returns(mockBody);

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
        var entity = CreateEntityWithTransformAndRb(context);
        var mockBody = Substitute.For<IPhysicsBody2D>();
        world.CreateBody(Arg.Any<PhysicsBodyDef>()).Returns(mockBody);

        system.OnInit();
        system.OnShutdown();

        world.Received(1).DestroyBody(mockBody);
        bodyStore.Snapshot().ShouldBeEmpty();
    }

    [Fact]
    public void Dispose_DisposesPhysicsWorld()
    {
        var world = Substitute.For<IPhysicsWorld2D>();
        var system = new PhysicsSimulationSystem(world, new Context(), new PhysicsRuntimeBodyStore());

        system.Dispose();

        world.Received(1).Dispose();
    }

    [Fact]
    public void OnInit_WithCircleCollider_CreatesCircleFixture()
    {
        var (system, context, world, _) = CreateFullSystem();
        var entity = CreateEntityWithTransformAndRb(context);
        entity.AddComponent(new CircleCollider2DComponent { Radius = 1.25f });
        var mockBody = Substitute.For<IPhysicsBody2D>();
        world.CreateBody(Arg.Any<PhysicsBodyDef>()).Returns(mockBody);

        system.OnInit();

        mockBody.Received(1).CreateCircleFixture(Arg.Is<PhysicsCircleFixtureDef>(d =>
            d.Radius == 1.25f && !d.IsSensor));
    }

    [Fact]
    public void OnInit_WithEdgeCollider_CreatesEdgeFixture()
    {
        var (system, context, world, _) = CreateFullSystem();
        var entity = CreateEntityWithTransformAndRb(context);
        entity.AddComponent(new EdgeCollider2DComponent());
        var mockBody = Substitute.For<IPhysicsBody2D>();
        world.CreateBody(Arg.Any<PhysicsBodyDef>()).Returns(mockBody);

        system.OnInit();

        mockBody.Received(1).CreateEdgeFixture(Arg.Any<PhysicsEdgeFixtureDef>());
    }

    [Fact]
    public void OnUpdate_SyncsTransformForCircleCollider()
    {
        var (system, context, world, _) = CreateFullSystem();
        var entity = CreateEntityWithTransformAndRb(context);
        entity.AddComponent(new CircleCollider2DComponent());
        var transform = entity.GetComponent<TransformComponent>();

        var mockBody = Substitute.For<IPhysicsBody2D>();
        mockBody.Position.Returns(new Vector2(3, 4));
        mockBody.Angle.Returns(0.25f);
        mockBody.LinearVelocity.Returns(Vector2.Zero);
        world.CreateBody(Arg.Any<PhysicsBodyDef>()).Returns(mockBody);

        system.OnInit();
        system.OnUpdate(TimeSpan.FromSeconds(0.017));

        transform.Translation.X.ShouldBe(3);
        transform.Translation.Y.ShouldBe(4);
        transform.Rotation.Z.ShouldBe(0.25f);
    }

    [Fact]
    public void OnInit_PrefersBoxWhenMultipleCollidersPresent()
    {
        var (system, context, world, _) = CreateFullSystem();
        var entity = CreateEntityWithTransformAndRb(context);
        entity.AddComponent(new BoxCollider2DComponent());
        entity.AddComponent(new CircleCollider2DComponent());
        var mockBody = Substitute.For<IPhysicsBody2D>();
        world.CreateBody(Arg.Any<PhysicsBodyDef>()).Returns(mockBody);

        system.OnInit();

        mockBody.Received(1).CreateBoxFixture(Arg.Any<PhysicsBoxFixtureDef>());
        mockBody.DidNotReceive().CreateCircleFixture(Arg.Any<PhysicsCircleFixtureDef>());
    }

    [Fact]
    public void OnUpdate_UnchangedIdentity_DoesNotRecreateBody()
    {
        var (system, context, world, bodyStore) = CreateFullSystem();
        var entity = CreateEntityWithFullCollider(context);
        var mockBody = StubBody();
        world.CreateBody(Arg.Any<PhysicsBodyDef>()).Returns(mockBody);

        system.OnInit();
        system.OnUpdate(TimeSpan.FromSeconds(0.017));

        world.Received(1).CreateBody(Arg.Any<PhysicsBodyDef>());
        world.DidNotReceive().DestroyBody(Arg.Any<IPhysicsBody2D>());
        bodyStore.TryGet(entity.Id, out var stored).ShouldBeTrue();
        stored.ShouldBe(mockBody);
    }

    [Fact]
    public void OnUpdate_BodyTypeChange_RecreatesBody()
    {
        var (system, context, world, bodyStore) = CreateFullSystem();
        var entity = CreateEntityWithFullCollider(context);
        var (first, second) = ArrangeTwoBodies(world);

        system.OnInit();
        entity.GetComponent<RigidBody2DComponent>().BodyType = RigidBodyType.Kinematic;
        system.OnUpdate(TimeSpan.FromSeconds(0.017));

        world.Received(1).DestroyBody(first);
        world.Received().CreateBody(Arg.Is<PhysicsBodyDef>(d => d.MotionType == PhysicsBodyMotionType.Kinematic));
        bodyStore.TryGet(entity.Id, out var stored).ShouldBeTrue();
        stored.ShouldBe(second);
    }

    [Fact]
    public void OnUpdate_BoxSizeChange_RecreatesBody()
    {
        var (system, context, world, bodyStore) = CreateFullSystem();
        var entity = CreateEntityWithFullCollider(context);
        var (first, second) = ArrangeTwoBodies(world);

        system.OnInit();
        entity.GetComponent<BoxCollider2DComponent>().Size = new Vector2(2f, 2f);
        system.OnUpdate(TimeSpan.FromSeconds(0.017));

        world.Received(1).DestroyBody(first);
        second.Received(1).CreateBoxFixture(Arg.Is<PhysicsBoxFixtureDef>(d => d.HalfWidth == 2f && d.HalfHeight == 2f));
        bodyStore.TryGet(entity.Id, out var stored).ShouldBeTrue();
        stored.ShouldBe(second);
    }

    [Fact]
    public void OnUpdate_ScaleChange_RecreatesBody()
    {
        var (system, context, world, _) = CreateFullSystem();
        var entity = CreateEntityWithFullCollider(context);
        var (first, second) = ArrangeTwoBodies(world);

        system.OnInit();
        entity.GetComponent<TransformComponent>().Scale = new Vector3(2f);
        system.OnUpdate(TimeSpan.FromSeconds(0.017));

        world.Received(1).DestroyBody(first);
        second.Received(1).CreateBoxFixture(Arg.Is<PhysicsBoxFixtureDef>(d => d.HalfWidth == 1f && d.HalfHeight == 1f));
    }

    [Fact]
    public void OnUpdate_GravityScaleChange_RecreatesBody()
    {
        var (system, context, world, _) = CreateFullSystem();
        var entity = CreateEntityWithFullCollider(context);
        ArrangeTwoBodies(world);

        system.OnInit();
        entity.GetComponent<RigidBody2DComponent>().GravityScale = 0f;
        system.OnUpdate(TimeSpan.FromSeconds(0.017));

        world.Received().CreateBody(Arg.Is<PhysicsBodyDef>(d => d.GravityScale == 0f));
        world.Received(1).DestroyBody(Arg.Any<IPhysicsBody2D>());
    }

    [Fact]
    public void OnUpdate_FixedRotationChange_RecreatesBody()
    {
        var (system, context, world, _) = CreateFullSystem();
        var entity = CreateEntityWithFullCollider(context);
        ArrangeTwoBodies(world);

        system.OnInit();
        entity.GetComponent<RigidBody2DComponent>().FixedRotation = true;
        system.OnUpdate(TimeSpan.FromSeconds(0.017));

        world.Received().CreateBody(Arg.Is<PhysicsBodyDef>(d => d.FixedRotation));
        world.Received(1).DestroyBody(Arg.Any<IPhysicsBody2D>());
    }

    [Fact]
    public void OnUpdate_DensityChange_RecreatesBody()
    {
        var (system, context, world, _) = CreateFullSystem();
        var entity = CreateEntityWithFullCollider(context);
        var (first, second) = ArrangeTwoBodies(world);

        system.OnInit();
        entity.GetComponent<BoxCollider2DComponent>().Density = 4f;
        system.OnUpdate(TimeSpan.FromSeconds(0.017));

        world.Received(1).DestroyBody(first);
        second.Received(1).CreateBoxFixture(Arg.Is<PhysicsBoxFixtureDef>(d => d.Density == 4f));
    }

    [Fact]
    public void OnUpdate_IsTriggerChange_RecreatesBody()
    {
        var (system, context, world, _) = CreateFullSystem();
        var entity = CreateEntityWithFullCollider(context);
        var (first, second) = ArrangeTwoBodies(world);

        system.OnInit();
        entity.GetComponent<BoxCollider2DComponent>().IsTrigger = true;
        system.OnUpdate(TimeSpan.FromSeconds(0.017));

        world.Received(1).DestroyBody(first);
        second.Received(1).CreateBoxFixture(Arg.Is<PhysicsBoxFixtureDef>(d => d.IsSensor));
    }

    [Fact]
    public void OnUpdate_BoxToCircle_RecreatesWithCircleFixture()
    {
        var (system, context, world, bodyStore) = CreateFullSystem();
        var entity = CreateEntityWithFullCollider(context);
        var (first, second) = ArrangeTwoBodies(world);

        system.OnInit();
        entity.RemoveComponent<BoxCollider2DComponent>();
        entity.AddComponent(new CircleCollider2DComponent { Radius = 1.25f });
        system.OnUpdate(TimeSpan.FromSeconds(0.017));

        world.Received(1).DestroyBody(first);
        first.DidNotReceive().CreateCircleFixture(Arg.Any<PhysicsCircleFixtureDef>());
        second.Received(1).CreateCircleFixture(Arg.Is<PhysicsCircleFixtureDef>(d => d.Radius == 1.25f));
        bodyStore.TryGet(entity.Id, out var stored).ShouldBeTrue();
        stored.ShouldBe(second);
    }

    [Fact]
    public void OnUpdate_ColliderAddedAfterCreate_AttachesFixture()
    {
        var (system, context, world, bodyStore) = CreateFullSystem();
        var entity = CreateEntityWithTransformAndRb(context);
        var (first, second) = ArrangeTwoBodies(world);

        system.OnInit();
        entity.AddComponent<BoxCollider2DComponent>();
        system.OnUpdate(TimeSpan.FromSeconds(0.017));

        world.Received(1).DestroyBody(first);
        first.DidNotReceive().CreateBoxFixture(Arg.Any<PhysicsBoxFixtureDef>());
        second.Received(1).CreateBoxFixture(Arg.Any<PhysicsBoxFixtureDef>());
        bodyStore.TryGet(entity.Id, out var stored).ShouldBeTrue();
        stored.ShouldBe(second);
    }

    [Fact]
    public void OnUpdate_EdgePointsChange_RecreatesBody()
    {
        var (system, context, world, _) = CreateFullSystem();
        var entity = CreateEntityWithTransformAndRb(context);
        entity.AddComponent(new EdgeCollider2DComponent());
        var (first, second) = ArrangeTwoBodies(world);

        system.OnInit();
        entity.GetComponent<EdgeCollider2DComponent>().Points = [new Vector2(-2f, 0f), new Vector2(2f, 0f)];
        system.OnUpdate(TimeSpan.FromSeconds(0.017));

        world.Received(1).DestroyBody(first);
        second.Received(1).CreateEdgeFixture(Arg.Any<PhysicsEdgeFixtureDef>());
    }

    private static (PhysicsSimulationSystem System, IContext Context, IPhysicsWorld2D World, PhysicsRuntimeBodyStore BodyStore) CreateFullSystem()
    {
        var world = Substitute.For<IPhysicsWorld2D>();
        var context = new Context();
        var bodyStore = new PhysicsRuntimeBodyStore();
        return (new PhysicsSimulationSystem(world, context, bodyStore), context, world, bodyStore);
    }

    private static Entity CreateEntityWithTransformAndRb(IContext context)
    {
        var entity = Entity.Create(1, "test");
        entity.AddComponent<RigidBody2DComponent>();
        entity.AddComponent<TransformComponent>();
        context.Register(entity);
        return entity;
    }

    private static Entity CreateEntityWithFullCollider(IContext context)
    {
        var entity = CreateEntityWithTransformAndRb(context);
        entity.AddComponent<BoxCollider2DComponent>();
        return entity;
    }

    private static (IPhysicsBody2D First, IPhysicsBody2D Second) ArrangeTwoBodies(IPhysicsWorld2D world)
    {
        var first = StubBody();
        var second = StubBody();
        world.CreateBody(Arg.Any<PhysicsBodyDef>()).Returns(first, second);
        return (first, second);
    }

    private static IPhysicsBody2D StubBody()
    {
        var body = Substitute.For<IPhysicsBody2D>();
        body.Position.Returns(Vector2.Zero);
        body.Angle.Returns(0f);
        body.LinearVelocity.Returns(Vector2.Zero);
        return body;
    }
}
