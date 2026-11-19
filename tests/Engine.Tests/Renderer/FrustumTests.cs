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

    [Fact]
    public void IsOutside_WorldBounds_MatchesMatrixOverloadForAxisAligned()
    {
        Frustum.TryFromClip(Ortho(), out var frustum).ShouldBeTrue();
        var world = Matrix4x4.CreateTranslation(3f, 0f, 0f);
        Aabb.WorldMinMax(world, Aabb.UnitCube, out var min, out var max);
        var worldBounds = new Aabb(min, max);

        frustum.IsOutside(worldBounds).ShouldBe(frustum.IsOutside(world, Aabb.UnitCube));
        frustum.IsOutside(worldBounds).ShouldBeTrue();
    }

    [Fact]
    public void IsOutside_WorldBounds_NonFiniteIsFalse()
    {
        Frustum.TryFromClip(Perspective(), out var frustum).ShouldBeTrue();
        var broken = new Aabb(new Vector3(float.NaN, -1f, -1f), Vector3.One);
        frustum.IsOutside(broken).ShouldBeFalse();
    }

    [Fact]
    public void WorldMinMax_InvertedLocalBox_StaysOrdered()
    {
        var inverted = new Aabb(new Vector3(300f, 1f, 1f), new Vector3(-10f, -1f, -1f));

        Aabb.WorldMinMax(Matrix4x4.Identity, inverted, out var min, out var max);

        (min.X < max.X && min.Y < max.Y && min.Z < max.Z).ShouldBeTrue();
        Aabb.ClosestPointDistanceSquared(Vector3.Zero, Matrix4x4.Identity, inverted).ShouldBe(0f);
    }

    [Fact]
    public void ClosestPointDistanceSquared_WorldBounds_ClampsToBox()
    {
        var box = new Aabb(new Vector3(-1f), new Vector3(1f));

        Aabb.ClosestPointDistanceSquared(new Vector3(3f, 0f, 0f), box).ShouldBe(4f);
        Aabb.ClosestPointDistanceSquared(new Vector3(0.5f, 0f, 0f), box).ShouldBe(0f);
        Aabb.ContainsPoint(Vector3.Zero, box).ShouldBeTrue();
        Aabb.ContainsPoint(new Vector3(3f, 0f, 0f), box).ShouldBeFalse();
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
