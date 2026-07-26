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
}
