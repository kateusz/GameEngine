using System.Numerics;
using Engine.Renderer;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class LightingMathTests
{
    [Fact]
    public void NormalizeDirection_UnitVector_ReturnsSameDirection()
    {
        var direction = new Vector3(1, 0, 0);

        LightingMath.NormalizeDirection(direction).ShouldBe(direction);
    }

    [Fact]
    public void NormalizeDirection_ZeroVector_ReturnsDefaultDown()
    {
        LightingMath.NormalizeDirection(Vector3.Zero).ShouldBe(LightingMath.DefaultDirection);
    }

    [Fact]
    public void NormalizeDirection_NearZeroVector_ReturnsDefaultDown()
    {
        var direction = new Vector3(1e-7f, 0f, 0f);

        LightingMath.NormalizeDirection(direction).ShouldBe(LightingMath.DefaultDirection);
    }

    [Fact]
    public void TryFitDirectionalShadow_Perspective_ContainsFrustumCorners()
    {
        var forward = Vector3.Normalize(new Vector3(0f, -0.3f, -1f));
        var eye = new Vector3(0f, 2f, 5f);
        var view = Matrix4x4.CreateLookAt(eye, eye + forward, Vector3.UnitY);
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 3f, 16f / 9f, 0.1f, 40f);
        var viewProjection = view * projection;

        LightingMath.TryFitDirectionalShadow(viewProjection, new Vector3(0f, -1f, 0f), out var light)
            .ShouldBeTrue();

        foreach (var z in new[] { 0f, 1f })
        foreach (var y in new[] { -1f, 1f })
        foreach (var x in new[] { -1f, 1f })
        {
            Matrix4x4.Invert(viewProjection, out var inverse);
            var world = Vector4.Transform(new Vector4(x, y, z, 1f), inverse);
            world /= world.W;
            var clip = Vector4.Transform(world, light);
            clip /= clip.W;
            clip.X.ShouldBeInRange(-1.01f, 1.01f);
            clip.Y.ShouldBeInRange(-1.01f, 1.01f);
            clip.Z.ShouldBeInRange(-0.01f, 1.01f);
        }
    }

    [Fact]
    public void TryFitDirectionalShadow_SubTexelTranslation_KeepsMatrix()
    {
        var first = ViewProjection(new Vector3(0.5f, 2f, 5f));
        var second = ViewProjection(new Vector3(0.5001f, 2f, 5f));

        LightingMath.TryFitDirectionalShadow(first, new Vector3(0f, -1f, 0f), out var a).ShouldBeTrue();
        LightingMath.TryFitDirectionalShadow(second, new Vector3(0f, -1f, 0f), out var b).ShouldBeTrue();

        a.M11.ShouldBe(b.M11, 1e-4f);
        a.M22.ShouldBe(b.M22, 1e-4f);
        a.M41.ShouldBe(b.M41, 1e-4f);
        a.M42.ShouldBe(b.M42, 1e-4f);
    }

    [Fact]
    public void TryFitDirectionalShadow_ZeroMatrix_ReturnsFalse()
    {
        LightingMath.TryFitDirectionalShadow(default, new Vector3(0f, -1f, 0f), out var light)
            .ShouldBeFalse();
        light.ShouldBe(Matrix4x4.Identity);
    }

    [Fact]
    public void TryFitDirectionalShadow_EditorFarPlane_KeepsUnitOcclusionAcrossASmallYaw()
    {
        var lightDirection = new Vector3(0f, -1f, 0f);
        foreach (var yaw in new[] { 0.40f, 0.42f })
        {
            var viewProjection = EditorViewProjection(yaw, pitch: 0.3f, distance: 10f);
            LightingMath.TryFitDirectionalShadow(viewProjection, lightDirection, out var light).ShouldBeTrue();

            var caster = WindowDepth(light, new Vector3(0f, 1f, 0f));
            var receiver = WindowDepth(light, new Vector3(0f, 0f, 0f));
            (receiver - 0.002f > caster).ShouldBeTrue();
        }
    }

    private static Matrix4x4 EditorViewProjection(float yaw, float pitch, float distance)
    {
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(
            45f * MathF.PI / 180f, 16f / 9f, 0.1f, 1000f);
        var orientation = Quaternion.CreateFromYawPitchRoll(-yaw, -pitch, 0f);
        var forward = Vector3.Transform(-Vector3.UnitZ, orientation);
        var position = -forward * distance;
        var rotation = Matrix4x4.CreateFromQuaternion(orientation);
        var transform = rotation * Matrix4x4.CreateTranslation(position);
        Matrix4x4.Invert(transform, out var view);
        return view * projection;
    }

    private static float WindowDepth(Matrix4x4 light, Vector3 world)
    {
        var clip = Vector4.Transform(new Vector4(world, 1f), light);
        return clip.Z / clip.W * 0.5f + 0.5f;
    }

    private static Matrix4x4 ViewProjection(Vector3 eye)
    {
        var forward = Vector3.Normalize(new Vector3(0f, -0.3f, -1f));
        var view = Matrix4x4.CreateLookAt(eye, eye + forward, Vector3.UnitY);
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 3f, 16f / 9f, 0.1f, 100f);
        return view * projection;
    }
}
