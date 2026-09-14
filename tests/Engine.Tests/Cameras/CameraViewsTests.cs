using System.Numerics;
using Engine.Renderer.Pipeline;
using Engine.Scene;
using Engine.Scene.Cameras;
using Shouldly;

namespace Engine.Tests.Cameras;

public class CameraViewsTests
{
    [Fact]
    public void TryFrom_IdentityTransform_ReturnsView()
    {
        var camera = new SceneCamera();

        CameraViews.TryFrom(camera, Matrix4x4.Identity, out var view).ShouldBeTrue();
        view.ShouldNotBe(default(SceneView));
        view.ViewPosition.ShouldBe(Vector3.Zero);
    }

    [Fact]
    public void TryFrom_NonInvertibleTransform_ReturnsFalse()
    {
        var camera = new SceneCamera();

        CameraViews.TryFrom(camera, default, out var view).ShouldBeFalse();
        view.ShouldBe(default(SceneView));
    }
}
