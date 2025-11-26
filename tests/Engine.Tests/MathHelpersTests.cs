using System.Numerics;
using Bogus;
using Engine.Math;
using Shouldly;

namespace Engine.Tests;

public class MathHelpersTests
{
    private readonly Faker _faker = new();

    #region Angle Conversion Tests

    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(MathF.PI, 180f)]
    [InlineData(MathF.PI * 2, 360f)]
    [InlineData(MathF.PI / 2, 90f)]
    [InlineData(-MathF.PI, -180f)]
    [InlineData(MathF.PI / 4, 45f)]
    public void RadiansToDegrees_ShouldConvertCorrectly(float radians, float expectedDegrees)
    {
        // Act
        var result = MathHelpers.RadiansToDegrees(radians);

        // Assert
        result.ShouldBe(expectedDegrees, 0.0001f);
    }

    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(180f, MathF.PI)]
    [InlineData(360f, MathF.PI * 2)]
    [InlineData(90f, MathF.PI / 2)]
    [InlineData(-180f, -MathF.PI)]
    [InlineData(45f, MathF.PI / 4)]
    public void DegreesToRadians_ShouldConvertCorrectly(float degrees, float expectedRadians)
    {
        // Act
        var result = MathHelpers.DegreesToRadians(degrees);

        // Assert
        result.ShouldBe(expectedRadians, 0.0001f);
    }

    [Fact]
    public void RadiansToDegrees_AndBack_ShouldReturnOriginalValue()
    {
        // Arrange
        var originalRadians = _faker.Random.Float(-10f, 10f);

        // Act
        var degrees = MathHelpers.RadiansToDegrees(originalRadians);
        var backToRadians = MathHelpers.DegreesToRadians(degrees);

        // Assert
        backToRadians.ShouldBe(originalRadians, 0.0001f);
    }

    [Fact]
    public void DegreesToRadians_AndBack_ShouldReturnOriginalValue()
    {
        // Arrange
        var originalDegrees = _faker.Random.Float(-360f, 360f);

        // Act
        var radians = MathHelpers.DegreesToRadians(originalDegrees);
        var backToDegrees = MathHelpers.RadiansToDegrees(radians);

        // Assert
        backToDegrees.ShouldBe(originalDegrees, 0.0001f);
    }

    #endregion

    #region Vector Angle Conversion Tests

    [Fact]
    public void ToDegrees_Vector3_ShouldConvertAllComponents()
    {
        // Arrange
        var radiansVector = new Vector3(MathF.PI, MathF.PI / 2, MathF.PI / 4);

        // Act
        var result = MathHelpers.ToDegrees(radiansVector);

        // Assert
        result.X.ShouldBe(180f, 0.0001f);
        result.Y.ShouldBe(90f, 0.0001f);
        result.Z.ShouldBe(45f, 0.0001f);
    }

    [Fact]
    public void ToRadians_Vector3_ShouldConvertAllComponents()
    {
        // Arrange
        var degreesVector = new Vector3(180f, 90f, 45f);

        // Act
        var result = MathHelpers.ToRadians(degreesVector);

        // Assert
        result.X.ShouldBe(MathF.PI, 0.0001f);
        result.Y.ShouldBe(MathF.PI / 2, 0.0001f);
        result.Z.ShouldBe(MathF.PI / 4, 0.0001f);
    }

    [Fact]
    public void ToRadians_AndBackToDegrees_Vector3_ShouldReturnOriginal()
    {
        // Arrange
        var original = new Vector3(
            _faker.Random.Float(-180f, 180f),
            _faker.Random.Float(-180f, 180f),
            _faker.Random.Float(-180f, 180f)
        );

        // Act
        var radians = MathHelpers.ToRadians(original);
        var backToDegrees = MathHelpers.ToDegrees(radians);

        // Assert
        backToDegrees.X.ShouldBe(original.X, 0.0001f);
        backToDegrees.Y.ShouldBe(original.Y, 0.0001f);
        backToDegrees.Z.ShouldBe(original.Z, 0.0001f);
    }

    #endregion

    #region Quaternion Tests

    [Fact]
    public void QuaternionFromEuler_ZeroRotation_ShouldReturnIdentityQuaternion()
    {
        // Arrange
        var euler = Vector3.Zero;

        // Act
        var result = MathHelpers.QuaternionFromEuler(euler);

        // Assert
        result.W.ShouldBe(1f, 0.0001f);
        result.X.ShouldBe(0f, 0.0001f);
        result.Y.ShouldBe(0f, 0.0001f);
        result.Z.ShouldBe(0f, 0.0001f);
    }

    [Fact]
    public void QuaternionFromEuler_90DegreesX_ShouldCreateCorrectQuaternion()
    {
        // Arrange
        var euler = new Vector3(MathF.PI / 2, 0, 0);

        // Act
        var result = MathHelpers.QuaternionFromEuler(euler);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.W.ShouldBe(MathF.Sqrt(2) / 2, 0.0001f),
            () => result.X.ShouldBe(MathF.Sqrt(2) / 2, 0.0001f),
            () => result.Y.ShouldBe(0f, 0.0001f),
            () => result.Z.ShouldBe(0f, 0.0001f)
        );
    }

    [Fact]
    public void QuaternionFromEuler_90DegreesY_ShouldCreateCorrectQuaternion()
    {
        // Arrange
        var euler = new Vector3(0, MathF.PI / 2, 0);

        // Act
        var result = MathHelpers.QuaternionFromEuler(euler);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.W.ShouldBe(MathF.Sqrt(2) / 2, 0.0001f),
            () => result.X.ShouldBe(0f, 0.0001f),
            () => result.Y.ShouldBe(MathF.Sqrt(2) / 2, 0.0001f),
            () => result.Z.ShouldBe(0f, 0.0001f)
        );
    }

    [Fact]
    public void QuaternionFromEuler_90DegreesZ_ShouldCreateCorrectQuaternion()
    {
        // Arrange
        var euler = new Vector3(0, 0, MathF.PI / 2);

        // Act
        var result = MathHelpers.QuaternionFromEuler(euler);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.W.ShouldBe(MathF.Sqrt(2) / 2, 0.0001f),
            () => result.X.ShouldBe(0f, 0.0001f),
            () => result.Y.ShouldBe(0f, 0.0001f),
            () => result.Z.ShouldBe(MathF.Sqrt(2) / 2, 0.0001f)
        );
    }

    [Fact]
    public void QuaternionFromEuler_ShouldBeNormalized()
    {
        // Arrange
        var euler = new Vector3(
            _faker.Random.Float(-MathF.PI, MathF.PI),
            _faker.Random.Float(-MathF.PI, MathF.PI),
            _faker.Random.Float(-MathF.PI, MathF.PI)
        );

        // Act
        var result = MathHelpers.QuaternionFromEuler(euler);
        var length = MathF.Sqrt(result.W * result.W + result.X * result.X + result.Y * result.Y + result.Z * result.Z);

        // Assert
        length.ShouldBe(1f, 0.0001f);
    }

    #endregion

    #region Matrix From Quaternion Tests

    [Fact]
    public void MatrixFromQuaternion_IdentityQuaternion_ShouldReturnIdentityMatrix()
    {
        // Arrange
        var quaternion = Quaternion.Identity;

        // Act
        var result = MathHelpers.MatrixFromQuaternion(quaternion);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.M11.ShouldBe(1f, 0.0001f),
            () => result.M22.ShouldBe(1f, 0.0001f),
            () => result.M33.ShouldBe(1f, 0.0001f),
            () => result.M44.ShouldBe(1f, 0.0001f),
            () => result.M12.ShouldBe(0f, 0.0001f),
            () => result.M13.ShouldBe(0f, 0.0001f),
            () => result.M21.ShouldBe(0f, 0.0001f),
            () => result.M23.ShouldBe(0f, 0.0001f),
            () => result.M31.ShouldBe(0f, 0.0001f),
            () => result.M32.ShouldBe(0f, 0.0001f)
        );
    }

    [Fact]
    public void MatrixFromQuaternion_ShouldCreateOrthogonalMatrix()
    {
        // Arrange
        var quaternion = new Quaternion(0.5f, 0.5f, 0.5f, 0.5f);

        // Act
        var result = MathHelpers.MatrixFromQuaternion(quaternion);

        // Assert - Check orthogonality: row vectors should be perpendicular
        var row1 = new Vector3(result.M11, result.M12, result.M13);
        var row2 = new Vector3(result.M21, result.M22, result.M23);
        var row3 = new Vector3(result.M31, result.M32, result.M33);

        Vector3.Dot(row1, row2).ShouldBe(0f, 0.0001f);
        Vector3.Dot(row1, row3).ShouldBe(0f, 0.0001f);
        Vector3.Dot(row2, row3).ShouldBe(0f, 0.0001f);
    }

    [Fact]
    public void MatrixFromQuaternion_ShouldHaveUnitLengthRows()
    {
        // Arrange
        var quaternion = Quaternion.CreateFromYawPitchRoll(0.5f, 0.3f, 0.2f);

        // Act
        var result = MathHelpers.MatrixFromQuaternion(quaternion);

        // Assert
        var row1 = new Vector3(result.M11, result.M12, result.M13);
        var row2 = new Vector3(result.M21, result.M22, result.M23);
        var row3 = new Vector3(result.M31, result.M32, result.M33);

        row1.Length().ShouldBe(1f, 0.0001f);
        row2.Length().ShouldBe(1f, 0.0001f);
        row3.Length().ShouldBe(1f, 0.0001f);
    }

    #endregion

    #region Matrix Decomposition Tests

    [Fact]
    public void DecomposeTransform_IdentityMatrix_ShouldReturnIdentityTransform()
    {
        // Arrange
        var matrix = Matrix4x4.Identity;

        // Act
        var success = MathHelpers.DecomposeTransform(matrix, out var translation, out var rotation, out var scale);

        // Assert
        success.ShouldBeTrue();
        translation.ShouldBe(Vector3.Zero);
        rotation.ShouldBe(Vector3.Zero);
        scale.ShouldBe(Vector3.One);
    }

    [Fact]
    public void DecomposeTransform_TranslationOnly_ShouldExtractTranslation()
    {
        // Arrange
        var expectedTranslation = new Vector3(10f, 20f, 30f);
        var matrix = Matrix4x4.CreateTranslation(expectedTranslation);

        // Act
        var success = MathHelpers.DecomposeTransform(matrix, out var translation, out var rotation, out var scale);

        // Assert
        success.ShouldBeTrue();
        translation.X.ShouldBe(expectedTranslation.X, 0.0001f);
        translation.Y.ShouldBe(expectedTranslation.Y, 0.0001f);
        translation.Z.ShouldBe(expectedTranslation.Z, 0.0001f);
        scale.ShouldBe(Vector3.One);
    }

    [Fact]
    public void DecomposeTransform_ScaleOnly_ShouldExtractScale()
    {
        // Arrange
        var expectedScale = new Vector3(2f, 3f, 4f);
        var matrix = Matrix4x4.CreateScale(expectedScale);

        // Act
        var success = MathHelpers.DecomposeTransform(matrix, out var translation, out var rotation, out var scale);

        // Assert
        success.ShouldBeTrue();
        scale.X.ShouldBe(expectedScale.X, 0.0001f);
        scale.Y.ShouldBe(expectedScale.Y, 0.0001f);
        scale.Z.ShouldBe(expectedScale.Z, 0.0001f);
        translation.ShouldBe(Vector3.Zero);
    }

    [Fact]
    public void DecomposeTransform_UniformScale_ShouldExtractCorrectScale()
    {
        // Arrange
        var expectedScale = 2.5f;
        var matrix = Matrix4x4.CreateScale(expectedScale);

        // Act
        var success = MathHelpers.DecomposeTransform(matrix, out var translation, out var rotation, out var scale);

        // Assert
        success.ShouldBeTrue();
        scale.X.ShouldBe(expectedScale, 0.0001f);
        scale.Y.ShouldBe(expectedScale, 0.0001f);
        scale.Z.ShouldBe(expectedScale, 0.0001f);
    }

    [Fact]
    public void DecomposeTransform_TranslationAndScale_ShouldExtractBoth()
    {
        // Arrange
        var expectedTranslation = new Vector3(5f, 10f, 15f);
        var expectedScale = new Vector3(2f, 2f, 2f);
        var matrix = Matrix4x4.CreateScale(expectedScale) * Matrix4x4.CreateTranslation(expectedTranslation);

        // Act
        var success = MathHelpers.DecomposeTransform(matrix, out var translation, out var rotation, out var scale);

        // Assert
        success.ShouldBeTrue();
        translation.X.ShouldBe(expectedTranslation.X, 0.0001f);
        translation.Y.ShouldBe(expectedTranslation.Y, 0.0001f);
        translation.Z.ShouldBe(expectedTranslation.Z, 0.0001f);
        scale.X.ShouldBe(expectedScale.X, 0.0001f);
        scale.Y.ShouldBe(expectedScale.Y, 0.0001f);
        scale.Z.ShouldBe(expectedScale.Z, 0.0001f);
    }

    [Fact]
    public void DecomposeTransform_InvalidMatrix_WithZeroM44_ShouldReturnFalse()
    {
        // Arrange
        var matrix = Matrix4x4.Identity;
        matrix.M44 = 0f;

        // Act
        var success = MathHelpers.DecomposeTransform(matrix, out var translation, out var rotation, out var scale);

        // Assert
        success.ShouldBeFalse();
        translation.ShouldBe(Vector3.Zero);
        rotation.ShouldBe(Vector3.Zero);
        scale.ShouldBe(Vector3.One);
    }

    [Fact]
    public void DecomposeTransform_ComplexTransform_ShouldDecomposeCorrectly()
    {
        // Arrange
        var expectedTranslation = new Vector3(1f, 2f, 3f);
        var expectedScale = new Vector3(2f, 2f, 2f);
        var quaternion = Quaternion.CreateFromYawPitchRoll(0.1f, 0.2f, 0.3f);

        var matrix = Matrix4x4.CreateScale(expectedScale)
                   * Matrix4x4.CreateFromQuaternion(quaternion)
                   * Matrix4x4.CreateTranslation(expectedTranslation);

        // Act
        var success = MathHelpers.DecomposeTransform(matrix, out var translation, out var rotation, out var scale);

        // Assert
        success.ShouldBeTrue();
        translation.X.ShouldBe(expectedTranslation.X, 0.0001f);
        translation.Y.ShouldBe(expectedTranslation.Y, 0.0001f);
        translation.Z.ShouldBe(expectedTranslation.Z, 0.0001f);
        scale.X.ShouldBe(expectedScale.X, 0.01f);
        scale.Y.ShouldBe(expectedScale.Y, 0.01f);
        scale.Z.ShouldBe(expectedScale.Z, 0.01f);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void EulerToQuaternionToMatrix_ShouldProduceConsistentResults()
    {
        // Arrange
        var euler = new Vector3(0.5f, 0.3f, 0.2f);

        // Act
        var quaternion = MathHelpers.QuaternionFromEuler(euler);
        var matrix = MathHelpers.MatrixFromQuaternion(quaternion);
        var systemMatrix = Matrix4x4.CreateFromQuaternion(quaternion);

        // Assert - Our matrix should match System.Numerics
        matrix.M11.ShouldBe(systemMatrix.M11, 0.0001f);
        matrix.M12.ShouldBe(systemMatrix.M12, 0.0001f);
        matrix.M13.ShouldBe(systemMatrix.M13, 0.0001f);
        matrix.M21.ShouldBe(systemMatrix.M21, 0.0001f);
        matrix.M22.ShouldBe(systemMatrix.M22, 0.0001f);
        matrix.M23.ShouldBe(systemMatrix.M23, 0.0001f);
        matrix.M31.ShouldBe(systemMatrix.M31, 0.0001f);
        matrix.M32.ShouldBe(systemMatrix.M32, 0.0001f);
        matrix.M33.ShouldBe(systemMatrix.M33, 0.0001f);
    }

    #endregion
}
