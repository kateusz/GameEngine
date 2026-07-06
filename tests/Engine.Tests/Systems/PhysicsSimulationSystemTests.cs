using System.Numerics;
using ECS;
using Engine.Physics;
using Engine.Renderer.Cameras;
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
        world.Received(1).Step(CameraConfig.PhysicsTimestep, 6, 2);
    }

    [Fact]
    public void OnUpdate_AccumulatesTimeAndStepsMultipleTimes()
    {
        var (system, _, world, _) = CreateFullSystem();
        system.OnUpdate(TimeSpan.FromSeconds(0.051));
        world.Received(3).Step(CameraConfig.PhysicsTimestep, 6, 2);
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
