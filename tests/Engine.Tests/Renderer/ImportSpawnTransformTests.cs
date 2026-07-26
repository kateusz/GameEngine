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
}
