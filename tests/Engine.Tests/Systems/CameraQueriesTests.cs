using System.Numerics;
using ECS;
using Engine.Renderer.Pipeline;
using Engine.Scene;
using Engine.Scene.Cameras;
using Engine.Scene.Systems;
using SceneComponents;
using SceneComponents.Camera;
using Shouldly;

namespace Engine.Tests.Systems;

public class CameraQueriesTests
{
    [Fact]
    public void TryGetPrimaryView_WhenNoCamera_ReturnsFalse()
    {
        var context = new Context();

        CameraQueries.TryGetPrimaryView(context, out var view).ShouldBeFalse();
        view.ShouldBe(default);
    }

    [Fact]
    public void TryGetPrimaryView_WhenOnlyNonPrimary_ReturnsFalse()
    {
        var context = new Context();
        RegisterCamera(context, 1, primary: false);

        CameraQueries.TryGetPrimaryView(context, out _).ShouldBeFalse();
    }

    [Fact]
    public void TryGetPrimaryView_WhenPrimaryExists_ReturnsViewFromThatCamera()
    {
        var context = new Context();
        RegisterCamera(context, 1, primary: false);
        var primary = RegisterCamera(context, 2, primary: true);
        primary.GetComponent<TransformComponent>().SetWorldTransform(Matrix4x4.CreateTranslation(1, 2, 3));

        CameraQueries.TryGetPrimaryView(context, out var view).ShouldBeTrue();
        view.ShouldBe(ExpectedView(primary));
    }

    [Fact]
    public void TryGetPrimaryView_WhenCameraViewTransformSet_PrefersItOverWorldTransform()
    {
        var context = new Context();
        var entity = RegisterCamera(context, 1, primary: true);
        entity.GetComponent<TransformComponent>().SetWorldTransform(Matrix4x4.CreateTranslation(1, 2, 3));
        entity.GetComponent<CameraComponent>().CameraViewTransform = Matrix4x4.CreateTranslation(4, 5, 6);

        CameraQueries.TryGetPrimaryView(context, out var view).ShouldBeTrue();
        view.ShouldBe(ExpectedView(entity));
        view.ViewPosition.ShouldBe(new Vector3(4, 5, 6));
    }

    [Fact]
    public void TryGetPrimaryView_WhenNoTransform_UsesIdentity()
    {
        var context = new Context();
        var entity = Entity.Create(1, "primary");
        entity.AddComponent(new CameraComponent { Primary = true });
        context.Register(entity);

        CameraQueries.TryGetPrimaryView(context, out var view).ShouldBeTrue();
        view.ShouldBe(ExpectedView(entity));
        view.ViewPosition.ShouldBe(Vector3.Zero);
    }

    [Fact]
    public void TryGetPrimaryView_WhenMultiplePrimaries_UsesFirstInView()
    {
        var context = new Context();
        var a = RegisterCamera(context, 1, primary: true);
        var b = RegisterCamera(context, 2, primary: true);
        a.GetComponent<TransformComponent>().SetWorldTransform(Matrix4x4.CreateTranslation(1, 0, 0));
        b.GetComponent<TransformComponent>().SetWorldTransform(Matrix4x4.CreateTranslation(2, 0, 0));

        Entity? firstPrimary = null;
        foreach (var (entity, camera) in context.View<CameraComponent>())
        {
            if (!camera.Primary)
                continue;
            firstPrimary = entity;
            break;
        }

        CameraQueries.TryGetPrimaryView(context, out var view).ShouldBeTrue();
        view.ShouldBe(ExpectedView(firstPrimary!));
    }

    [Fact]
    public void TryGetPrimaryView_WhenPrimaryTransformNotInvertible_ReturnsFalse()
    {
        var context = new Context();
        var entity = RegisterCamera(context, 1, primary: true);
        entity.GetComponent<TransformComponent>().SetWorldTransform(default);

        CameraQueries.TryGetPrimaryView(context, out var view).ShouldBeFalse();
        view.ShouldBe(default);
    }

    private static Entity RegisterCamera(IContext context, int id, bool primary)
    {
        var entity = Entity.Create(id, primary ? "primary" : "other");
        entity.AddComponent(new CameraComponent { Primary = primary });
        entity.AddComponent<TransformComponent>();
        context.Register(entity);
        return entity;
    }

    private static SceneView ExpectedView(Entity entity)
    {
        var cameraComponent = entity.GetComponent<CameraComponent>();
        var scratch = new SceneCamera();
        scratch.Apply(cameraComponent);
        var transform = cameraComponent.CameraViewTransform
            ?? (entity.TryGetComponent<TransformComponent>(out var transformComponent)
                ? transformComponent.GetWorldTransform()
                : Matrix4x4.Identity);
        CameraViews.TryFrom(scratch, transform, out var expected).ShouldBeTrue();
        return expected;
    }
}
