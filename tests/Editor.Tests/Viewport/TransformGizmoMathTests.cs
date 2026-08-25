using System.Numerics;
using Editor.Features.Viewport.Gizmos;
using SceneComponents;
using Shouldly;

namespace Editor.Tests.Viewport;

public class TransformGizmoMathTests
{
    [Fact]
    public void TryApplyWorldMatrix_UnparentedTranslation_WritesLocalTranslation()
    {
        var transform = new TransformComponent(Vector3.Zero, Vector3.Zero, Vector3.One);
        transform.SetWorldTransform(transform.GetTransform());

        var world = Matrix4x4.CreateTranslation(4, 5, 6);
        TransformGizmoMath.TryApplyWorldMatrix(transform, world).ShouldBeTrue();

        transform.Translation.X.ShouldBe(4f, 0.001f);
        transform.Translation.Y.ShouldBe(5f, 0.001f);
        transform.Translation.Z.ShouldBe(6f, 0.001f);
        transform.Scale.X.ShouldBe(1f, 0.001f);
        transform.Scale.Y.ShouldBe(1f, 0.001f);
        transform.Scale.Z.ShouldBe(1f, 0.001f);
    }

    [Fact]
    public void TryApplyWorldMatrix_WithParent_WritesLocalRelativeToParent()
    {
        var transform = new TransformComponent(new Vector3(1, 0, 0), Vector3.Zero, Vector3.One);
        var parentWorld = Matrix4x4.CreateTranslation(10, 0, 0);
        transform.SetWorldTransform(transform.GetTransform() * parentWorld);

        var world = Matrix4x4.CreateTranslation(13, 2, 0);
        TransformGizmoMath.TryApplyWorldMatrix(transform, world).ShouldBeTrue();

        transform.Translation.X.ShouldBe(3f, 0.001f);
        transform.Translation.Y.ShouldBe(2f, 0.001f);
        transform.Translation.Z.ShouldBe(0f, 0.001f);
    }
}
