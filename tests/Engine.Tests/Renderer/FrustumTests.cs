using System.Numerics;
using Engine.Renderer;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class FrustumTests
{
    [Fact]
    public void IsOutside_PerspectiveUnitCubeAtOrigin_IsFalse()
    {
        Frustum.TryFromClip(Perspective(), out var frustum).ShouldBeTrue();
        frustum.IsOutside(Matrix4x4.Identity, Aabb.UnitCube).ShouldBeFalse();
    }

    [Fact]
    public void IsOutside_OrthoUnitCubeAtPositiveX_IsTrue()
    {
        Frustum.TryFromClip(Ortho(), out var frustum).ShouldBeTrue();
        frustum.IsOutside(Matrix4x4.CreateTranslation(3f, 0f, 0f), Aabb.UnitCube).ShouldBeTrue();
    }

    [Fact]
    public void IsOutside_PerspectiveUnitCubeBeyondFar_IsTrue()
    {
        Frustum.TryFromClip(Perspective(), out var frustum).ShouldBeTrue();
        frustum.IsOutside(Matrix4x4.CreateTranslation(0f, 0f, -200f), Aabb.UnitCube).ShouldBeTrue();
    }

    [Fact]
    public void IsOutside_PerspectiveUnitCubeStraddlingNear_IsFalse()
    {
        Frustum.TryFromClip(Perspective(), out var frustum).ShouldBeTrue();
        frustum.IsOutside(Matrix4x4.CreateTranslation(0f, 0f, 4.9f), Aabb.UnitCube).ShouldBeFalse();
    }

    [Fact]
    public void IsOutside_RotatedRodMissingOrtho_IsTrue()
    {
        Frustum.TryFromClip(Ortho(), out var frustum).ShouldBeTrue();
        // (3, -3) sits in the outside corner, where no single plane contains every corner.
        // Shift to +X so the rotated rod is wholly past the right face.
        var world = Matrix4x4.CreateRotationZ(MathF.PI / 4f) * Matrix4x4.CreateTranslation(6f, 0f, 0f);
        frustum.IsOutside(world, Rod).ShouldBeTrue();
    }

    [Fact]
    public void IsOutside_RotatedRodInOutsideCorner_IsFalse()
    {
        Frustum.TryFromClip(Ortho(), out var frustum).ShouldBeTrue();
        var world = Matrix4x4.CreateRotationZ(MathF.PI / 4f) * Matrix4x4.CreateTranslation(3f, -3f, 0f);
        frustum.IsOutside(world, Rod).ShouldBeFalse();
    }

    [Fact]
    public void IsOutside_RotatedRodAtOrigin_IsFalse()
    {
        Frustum.TryFromClip(Ortho(), out var frustum).ShouldBeTrue();
        frustum.IsOutside(Matrix4x4.Identity, Rod).ShouldBeFalse();
    }

    [Fact]
    public void IsOutside_NegativeScaleUnitCube_IsFalse()
    {
        Frustum.TryFromClip(Perspective(), out var frustum).ShouldBeTrue();
        frustum.IsOutside(Matrix4x4.CreateScale(-1f, -1f, -1f), Aabb.UnitCube).ShouldBeFalse();
    }

    [Fact]
    public void IsOutside_NonFiniteTranslation_IsFalse()
    {
        Frustum.TryFromClip(Perspective(), out var frustum).ShouldBeTrue();
        frustum.IsOutside(Matrix4x4.CreateTranslation(float.NaN, 0f, 0f), Aabb.UnitCube).ShouldBeFalse();
    }

    [Fact]
    public void TryFromClip_DefaultMatrix_Fails()
    {
        Frustum.TryFromClip(default, out _).ShouldBeFalse();
    }

    private static readonly Aabb Rod = new(new Vector3(-5f, -0.02f, -0.02f), new Vector3(5f, 0.02f, 0.02f));

    private static Matrix4x4 Perspective()
    {
        var view = Matrix4x4.CreateLookAt(new Vector3(0f, 0f, 5f), Vector3.Zero, Vector3.UnitY);
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 3f, 16f / 9f, 0.1f, 100f);
        return view * projection;
    }

    private static Matrix4x4 Ortho()
    {
        var view = Matrix4x4.CreateLookAt(new Vector3(0f, 0f, 5f), Vector3.Zero, Vector3.UnitY);
        var projection = Matrix4x4.CreateOrthographicOffCenter(-1f, 1f, -1f, 1f, 0.1f, 100f);
        return view * projection;
    }
}
