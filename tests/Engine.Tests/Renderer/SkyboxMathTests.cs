using System.Numerics;
using Engine.Renderer;
using Engine.Scene.Cameras;
using Shouldly;

namespace Engine.Tests.Renderer;

public class SkyboxMathTests
{
    [Fact]
    public void CenterNdc_MatchesCameraForward()
    {
        var camera = new EditorCamera();
        camera.SetViewportSize(1280f, 720f);

        SkyboxMath.TryInvertRotationViewProjection(
            camera.GetViewMatrix(),
            camera.GetProjectionMatrix(),
            out var invVp).ShouldBeTrue();

        var dir = SkyboxMath.DirectionFromNdc(invVp, 0f, 0f);
        var forward = camera.GetForwardDirection();

        Vector3.Dot(dir, forward).ShouldBe(1f, 0.02f);
    }

    [Fact]
    public void YawChange_RotatesCenterDirection()
    {
        var camera = new EditorCamera();
        camera.SetViewportSize(1280f, 720f);

        SkyboxMath.TryInvertRotationViewProjection(
            camera.GetViewMatrix(),
            camera.GetProjectionMatrix(),
            out var inv0).ShouldBeTrue();
        var dir0 = SkyboxMath.DirectionFromNdc(inv0, 0f, 0f);

        camera.SetYaw(1.0f);
        SkyboxMath.TryInvertRotationViewProjection(
            camera.GetViewMatrix(),
            camera.GetProjectionMatrix(),
            out var inv1).ShouldBeTrue();
        var dir1 = SkyboxMath.DirectionFromNdc(inv1, 0f, 0f);

        Vector3.Dot(dir0, dir1).ShouldBeLessThan(0.9f);
    }

    [Fact]
    public void CornerNdc_DirectionsAreFiniteUnitLength()
    {
        var camera = new EditorCamera();
        camera.SetViewportSize(1280f, 720f);
        SkyboxMath.TryInvertRotationViewProjection(
            camera.GetViewMatrix(),
            camera.GetProjectionMatrix(),
            out var inv).ShouldBeTrue();

        foreach (var (x, y) in new (float, float)[] { (1f, 1f), (-1f, -1f), (1f, -1f), (-1f, 1f) })
        {
            var dir = SkyboxMath.DirectionFromNdc(inv, x, y);
            float.IsFinite(dir.X).ShouldBeTrue($"ndc ({x},{y})");
            float.IsFinite(dir.Y).ShouldBeTrue();
            float.IsFinite(dir.Z).ShouldBeTrue();
            dir.Length().ShouldBe(1f, 0.01f);
        }
    }
}
