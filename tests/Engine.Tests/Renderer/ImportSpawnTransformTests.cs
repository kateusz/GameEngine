using System.Numerics;
using Engine.Renderer;
using Math;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class ImportSpawnTransformTests
{
    [Fact]
    public void FromLocalToRoot_CentimeterNodeScale_StripsSpawnScaleAndTranslation()
    {
        var matrix = Matrix4x4.CreateScale(100f) * Matrix4x4.CreateTranslation(0f, 387.92f, 0f);

        var (translation, _, scale) = ImportSpawnTransform.FromLocalToRoot(matrix, unitDownscaleFactor: 1f);

        scale.ShouldBe(Vector3.One);
        translation.Y.ShouldBe(3.8792f, 0.01f);
    }

    [Fact]
    public void FromLocalToRoot_MeterNode_LeavesTranslationUnscaled()
    {
        var matrix = Matrix4x4.CreateTranslation(0f, 1.8f, 0f);

        var (translation, _, scale) = ImportSpawnTransform.FromLocalToRoot(matrix, 1f);

        translation.Y.ShouldBe(1.8f, 0.01f);
        scale.ShouldBe(Vector3.One);
    }

    [Fact]
    public void FromLocalToRoot_FbxUpAxisConvertedRotation_MatchesExpectedQuaternion()
    {
        // Assimp ConvertToOpenGL: FBX Z-up → engine Y-up is −90° about X.
        var matrix = Matrix4x4.CreateRotationX(-MathF.PI / 2f);
        var expected = Quaternion.CreateFromAxisAngle(Vector3.UnitX, -MathF.PI / 2f);

        var (_, rotationEuler, scale) = ImportSpawnTransform.FromLocalToRoot(matrix, 1f);

        scale.ShouldBe(Vector3.One);
        var actual = MathHelpers.QuaternionFromEuler(rotationEuler);
        MathF.Abs(Quaternion.Dot(actual, expected)).ShouldBe(1f, 1e-4f);
    }
}
