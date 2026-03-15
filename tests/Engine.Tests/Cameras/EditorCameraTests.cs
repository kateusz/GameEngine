using System.Numerics;
using Engine.Renderer.Cameras;
using Shouldly;

namespace Engine.Tests.Cameras;

public class EditorCameraTests
{
    [Fact]
    public void DefaultPosition_IsAtDistanceAlongZ()
    {
        var camera = new EditorCamera();
        // Default: focal=(0,0,0), distance=10, pitch=0, yaw=0
        // Forward = -Z, so position = focal - forward * distance = (0,0,0) - (0,0,-1)*10 = (0,0,10)
        var pos = camera.GetPosition();
        pos.X.ShouldBe(0f, 0.001f);
        pos.Y.ShouldBe(0f, 0.001f);
        pos.Z.ShouldBe(CameraConfig.DefaultEditorDistance, 0.001f);
    }

    [Fact]
    public void ViewMatrix_IsInverseOfTransform()
    {
        var camera = new EditorCamera();
        camera.SetPitch(0.5f);
        camera.SetYaw(1.0f);

        var view = camera.GetViewMatrix();
        var position = camera.GetPosition();
        var orientation = camera.GetOrientation();

        var transform = Matrix4x4.CreateFromQuaternion(orientation)
                      * Matrix4x4.CreateTranslation(position);
        Matrix4x4.Invert(transform, out var expectedView);

        AssertMatricesEqual(view, expectedView, 0.001f);
    }

    [Fact]
    public void SetYaw_ChangesPosition()
    {
        var camera = new EditorCamera();
        var initialPos = camera.GetPosition();

        camera.SetYaw(0.5f);

        var newPos = camera.GetPosition();
        newPos.ShouldNotBe(initialPos);
    }

    [Fact]
    public void SetDistance_ClampsToRange()
    {
        var camera = new EditorCamera();

        camera.SetDistance(0.1f);
        camera.Distance.ShouldBe(CameraConfig.MinEditorDistance);

        camera.SetDistance(1000.0f);
        camera.Distance.ShouldBe(CameraConfig.MaxEditorDistance);

        camera.SetDistance(5.0f);
        camera.Distance.ShouldBe(5.0f);
    }

    [Fact]
    public void SetFocalPoint_MovesFocalPoint()
    {
        var camera = new EditorCamera();
        var target = new Vector3(5, 5, 0);

        camera.SetFocalPoint(target);

        camera.FocalPoint.ShouldBe(target);
    }

    [Fact]
    public void ViewProjectionMatrix_IsViewTimesProjection()
    {
        var camera = new EditorCamera();
        camera.SetPitch(0.3f);
        camera.SetYaw(0.7f);

        var vp = camera.GetViewProjectionMatrix();
        var expected = camera.GetViewMatrix() * camera.GetProjectionMatrix();

        AssertMatricesEqual(vp, expected, 0.001f);
    }

    [Fact]
    public void SetViewportSize_UpdatesProjection()
    {
        var camera = new EditorCamera();
        var proj1 = camera.GetProjectionMatrix();

        // Use 4:3 aspect ratio (different from default 16:9)
        camera.SetViewportSize(800, 600);
        var proj2 = camera.GetProjectionMatrix();

        proj1.ShouldNotBe(proj2);
    }

    [Fact]
    public void GetOrientation_DefaultIsIdentityLookingDown_NegZ()
    {
        var camera = new EditorCamera();
        var forward = camera.GetForwardDirection();

        // Default pitch=0, yaw=0: forward should be -Z
        forward.X.ShouldBe(0f, 0.001f);
        forward.Y.ShouldBe(0f, 0.001f);
        forward.Z.ShouldBe(-1f, 0.001f);
    }

    private static void AssertMatricesEqual(Matrix4x4 actual, Matrix4x4 expected, float tolerance)
    {
        actual.M11.ShouldBe(expected.M11, tolerance);
        actual.M12.ShouldBe(expected.M12, tolerance);
        actual.M13.ShouldBe(expected.M13, tolerance);
        actual.M14.ShouldBe(expected.M14, tolerance);
        actual.M21.ShouldBe(expected.M21, tolerance);
        actual.M22.ShouldBe(expected.M22, tolerance);
        actual.M23.ShouldBe(expected.M23, tolerance);
        actual.M24.ShouldBe(expected.M24, tolerance);
        actual.M31.ShouldBe(expected.M31, tolerance);
        actual.M32.ShouldBe(expected.M32, tolerance);
        actual.M33.ShouldBe(expected.M33, tolerance);
        actual.M34.ShouldBe(expected.M34, tolerance);
        actual.M41.ShouldBe(expected.M41, tolerance);
        actual.M42.ShouldBe(expected.M42, tolerance);
        actual.M43.ShouldBe(expected.M43, tolerance);
        actual.M44.ShouldBe(expected.M44, tolerance);
    }
}
