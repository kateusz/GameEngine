using System.Numerics;
using Engine.Renderer;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class LightSpaceMatrixTests
{
    [Fact]
    public void Create_PointAlongLookDirection_IsInsideClipVolume()
    {
        var matrix = LightSpaceMatrix.Create(new Vector3(0, -1, 0), Vector3.Zero, 20f);
        var midpoint = LightSpaceMatrix.TransformPoint(matrix, new Vector3(0, -50f, 0));

        midpoint.Z.ShouldBeInRange(-1f, 1f);
        LightSpaceMatrix.IsFinite(matrix).ShouldBeTrue();
    }

    [Fact]
    public void Create_PointBeyondFarPlane_IsOutsideClipVolume()
    {
        var matrix = LightSpaceMatrix.Create(new Vector3(0, -1, 0), Vector3.Zero, 20f);
        var beyond = LightSpaceMatrix.TransformPoint(matrix, new Vector3(0, -200f, 0));

        beyond.Z.ShouldBeGreaterThan(1f);
    }

    [Fact]
    public void Create_StraightDownDirection_ProducesFiniteMatrix()
    {
        var matrix = LightSpaceMatrix.Create(new Vector3(0, -1, 0), Vector3.Zero, 20f);
        LightSpaceMatrix.IsFinite(matrix).ShouldBeTrue();
    }

    [Fact]
    public void CreateCubemapFaces_ProducesSixFiniteMatrices()
    {
        var faces = LightSpaceMatrix.CreateCubemapFaces(new Vector3(0, 5, 0), 25f);
        faces.Length.ShouldBe(6);
        foreach (var face in faces)
            LightSpaceMatrix.IsFinite(face).ShouldBeTrue();
    }

    [Fact]
    public void IsFinite_NaNMatrix_ReturnsFalse()
    {
        var matrix = Matrix4x4.Identity;
        matrix.M11 = float.NaN;
        LightSpaceMatrix.IsFinite(matrix).ShouldBeFalse();
    }
}
