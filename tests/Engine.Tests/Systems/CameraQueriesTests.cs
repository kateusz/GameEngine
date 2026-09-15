using System.Numerics;
using ECS;
using Engine.Scene;
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

        CameraQueries.TryGetPrimaryView(context, new SceneCamera(), out var view).ShouldBeFalse();
        view.ShouldBe(default);
    }

    [Fact]
    public void TryGetPrimaryView_WhenOnlyNonPrimary_ReturnsFalse()
    {
        var context = new Context();
        RegisterCamera(context, 1, primary: false);

        CameraQueries.TryGetPrimaryView(context, new SceneCamera(), out _).ShouldBeFalse();
    }

    [Fact]
    public void TryGetPrimaryView_WhenPrimaryExists_ReturnsViewFromThatCamera()
    {
        var context = new Context();
        RegisterCamera(context, 1, primary: false);
        var primary = RegisterCamera(context, 2, primary: true);
        primary.GetComponent<TransformComponent>().SetWorldTransform(Matrix4x4.CreateTranslation(1, 2, 3));

        CameraQueries.TryGetPrimaryView(context, new SceneCamera(), out var view).ShouldBeTrue();
        view.ViewPosition.ShouldBe(new Vector3(1, 2, 3));
    }

    private static Entity RegisterCamera(IContext context, int id, bool primary)
    {
        var entity = Entity.Create(id, primary ? "primary" : "other");
        entity.AddComponent(new CameraComponent { Primary = primary });
        entity.AddComponent<TransformComponent>();
        context.Register(entity);
        return entity;
    }
}
