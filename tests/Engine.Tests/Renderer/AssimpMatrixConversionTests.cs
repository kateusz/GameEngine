using System.Numerics;
using Engine.Renderer.Models;
using Math;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class AssimpMatrixConversionTests
{
    /// <summary>
    /// Assimp aiMatrix4x4 is row-stored with translation in column 4 (a4,b4,c4 → M14,M24,M34).
    /// </summary>
    private static Matrix4x4 AssimpLayoutTranslation(float x, float y, float z) =>
        new(
            1, 0, 0, x,
            0, 1, 0, y,
            0, 0, 1, z,
            0, 0, 0, 1);

    [Fact]
    public void ToEngineMatrix_AssimpLayoutTranslation_DecomposesToSameVector()
    {
        var assimp = AssimpLayoutTranslation(3, -2, 7);
        var engine = AssimpModelImporter.ToEngineMatrix(assimp);

        MathHelpers.DecomposeTransform(engine, out var translation, out _, out _).ShouldBeTrue();
        translation.X.ShouldBe(3f, 0.001f);
        translation.Y.ShouldBe(-2f, 0.001f);
        translation.Z.ShouldBe(7f, 0.001f);
    }

    [Fact]
    public void ToEngineMatrix_SystemTranslation_DecomposesToSameVector()
    {
        var assimp = Matrix4x4.CreateTranslation(5, 1, -4);
        var engine = AssimpModelImporter.ToEngineMatrix(assimp);

        MathHelpers.DecomposeTransform(engine, out var translation, out _, out _).ShouldBeTrue();
        translation.X.ShouldBe(5f, 0.001f);
        translation.Y.ShouldBe(1f, 0.001f);
        translation.Z.ShouldBe(-4f, 0.001f);
    }

    [Fact]
    public void ToEngineMatrix_LegacyTranspose_WouldZeroSystemTranslation()
    {
        var assimp = Matrix4x4.CreateTranslation(5, 1, -4);
        var transposed = Matrix4x4.Transpose(assimp);

        MathHelpers.DecomposeTransform(transposed, out var translation, out _, out _).ShouldBeTrue();
        translation.ShouldBe(Vector3.Zero);
    }
}
