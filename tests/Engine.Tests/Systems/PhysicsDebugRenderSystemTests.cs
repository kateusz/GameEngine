using System.Numerics;
using ECS;
using Engine.Core;
using Engine.Physics;
using Engine.Renderer;
using Engine.Renderer.Pipeline;
using Engine.Scene;
using Engine.Scene.Cameras;
using Engine.Scene.Systems;
using NSubstitute;
using SceneComponents;
using SceneComponents.Physics;

namespace Engine.Tests.Systems;

public class PhysicsDebugRenderSystemTests
{
    [Fact]
    public void OnUpdate_WhenShowColliderBoundsFalse_DoesNotDraw()
    {
        var debugSettings = new DebugSettings { ShowColliderBounds = false };
        var (system, graphics2D, _, _, _) = CreateFullSystem(debugSettings);

        system.OnUpdate(TimeSpan.Zero);

        graphics2D.DidNotReceive().BeginScene(Arg.Any<Camera>(), Arg.Any<Matrix4x4>());
        graphics2D.DidNotReceive().BeginScene(Arg.Any<IViewCamera>());
        graphics2D.DidNotReceive().EndScene();
    }

    [Fact]
    public void OnUpdate_WithValidCameraAndColliderEntity_DrawsDebugVisuals()
    {
        var debugSettings = new DebugSettings { ShowColliderBounds = true };
        var (system, graphics2D, context, bodyStore, cameraProvider) = CreateFullSystem(debugSettings);

        cameraProvider.Camera.Returns(new SceneCamera());
        cameraProvider.Transform.Returns(Matrix4x4.Identity);

        var entity = Entity.Create(1, "test");
        entity.AddComponent<BoxCollider2DComponent>();
        entity.AddComponent<TransformComponent>();
        context.Register(entity);

        var body = Substitute.For<IPhysicsBody2D>();
        body.Position.Returns(Vector2.Zero);
        body.Angle.Returns(0f);
        body.IsEnabled().Returns(true);
        bodyStore.Set(1, body);

        system.OnUpdate(TimeSpan.Zero);

        graphics2D.Received(1).BeginScene(Arg.Any<Camera>(), Arg.Any<Matrix4x4>());
        graphics2D.Received(1).EndScene();
        graphics2D.Received(1).DrawRect(Arg.Any<Matrix4x4>(), Arg.Any<Vector4>(), 1);
    }

    [Fact]
    public void OnUpdate_WithCircleCollider_DrawsCircleOutline()
    {
        var debugSettings = new DebugSettings { ShowColliderBounds = true };
        var (system, graphics2D, context, bodyStore, cameraProvider) = CreateFullSystem(debugSettings);

        cameraProvider.Camera.Returns(new SceneCamera());
        cameraProvider.Transform.Returns(Matrix4x4.Identity);

        var entity = Entity.Create(2, "circle");
        entity.AddComponent(new CircleCollider2DComponent { Radius = 1f });
        entity.AddComponent<TransformComponent>();
        context.Register(entity);

        var body = Substitute.For<IPhysicsBody2D>();
        body.Position.Returns(Vector2.Zero);
        body.Angle.Returns(0f);
        body.IsEnabled().Returns(true);
        bodyStore.Set(2, body);

        system.OnUpdate(TimeSpan.Zero);

        graphics2D.Received().DrawLine(Arg.Any<Vector3>(), Arg.Any<Vector3>(), Arg.Any<Vector4>(), 2);
    }

    private static (PhysicsDebugRenderSystem, IGraphics2D, IContext, PhysicsRuntimeBodyStore, IPrimaryCameraProvider) CreateFullSystem(DebugSettings debugSettings)
    {
        var graphics2D = Substitute.For<IGraphics2D>();
        var context = new Context();
        var bodyStore = new PhysicsRuntimeBodyStore();
        var cameraProvider = Substitute.For<IPrimaryCameraProvider>();
        var system = new PhysicsDebugRenderSystem(graphics2D, context, debugSettings, bodyStore, cameraProvider);
        return (system, graphics2D, context, bodyStore, cameraProvider);
    }
}
