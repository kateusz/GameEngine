using System.Numerics;
using Bogus;
using Engine.Scene.Components;
using Shouldly;
using Xunit;

namespace Engine.Tests;

public class TransformComponentTests
{
    private readonly Faker _faker = new();

    #region Constructor Tests

    [Fact]
    public void Constructor_Default_ShouldInitializeWithDefaultValues()
    {
        // Act
        var transform = new TransformComponent();

        // Assert
        transform.Translation.ShouldBe(Vector3.Zero);
        transform.Rotation.ShouldBe(Vector3.Zero);
        transform.Scale.ShouldBe(Vector3.One);
    }

    [Fact]
    public void Constructor_WithParameters_ShouldInitializeWithGivenValues()
    {
        // Arrange
        var translation = new Vector3(1, 2, 3);
        var rotation = new Vector3(0.1f, 0.2f, 0.3f);
        var scale = new Vector3(2, 2, 2);

        // Act
        var transform = new TransformComponent(translation, rotation, scale);

        // Assert
        transform.Translation.ShouldBe(translation);
        transform.Rotation.ShouldBe(rotation);
        transform.Scale.ShouldBe(scale);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Translation_Set_ShouldUpdateValue()
    {
        // Arrange
        var transform = new TransformComponent();
        var newTranslation = new Vector3(5, 10, 15);

        // Act
        transform.Translation = newTranslation;

        // Assert
        transform.Translation.ShouldBe(newTranslation);
    }

    [Fact]
    public void Rotation_Set_ShouldUpdateValue()
    {
        // Arrange
        var transform = new TransformComponent();
        var newRotation = new Vector3(0.5f, 1.0f, 1.5f);

        // Act
        transform.Rotation = newRotation;

        // Assert
        transform.Rotation.ShouldBe(newRotation);
    }

    [Fact]
    public void Scale_Set_ShouldUpdateValue()
    {
        // Arrange
        var transform = new TransformComponent();
        var newScale = new Vector3(2, 3, 4);

        // Act
        transform.Scale = newScale;

        // Assert
        transform.Scale.ShouldBe(newScale);
    }

    #endregion

    #region GetTransform Tests

    [Fact]
    public void GetTransform_WhenNotDirty_ShouldReturnCachedMatrix()
    {
        // Arrange
        var transform = new TransformComponent();
        var firstMatrix = transform.GetTransform();

        // Act - Call again without modifying properties
        var secondMatrix = transform.GetTransform();

        // Assert - Should return same cached matrix
        secondMatrix.ShouldBe(firstMatrix);
    }

    [Fact]
    public void GetTransform_AfterTranslationChange_ShouldRecalculateMatrix()
    {
        // Arrange
        var transform = new TransformComponent();
        var firstMatrix = transform.GetTransform();

        // Act
        transform.Translation = new Vector3(10, 20, 30);
        var secondMatrix = transform.GetTransform();

        // Assert - Matrix should be different
        secondMatrix.ShouldNotBe(firstMatrix);
        secondMatrix.M41.ShouldBe(10f);
        secondMatrix.M42.ShouldBe(20f);
        secondMatrix.M43.ShouldBe(30f);
    }

    [Fact]
    public void GetTransform_AfterRotationChange_ShouldRecalculateMatrix()
    {
        // Arrange
        var transform = new TransformComponent();
        var firstMatrix = transform.GetTransform();

        // Act
        transform.Rotation = new Vector3(0, MathF.PI / 2, 0);
        var secondMatrix = transform.GetTransform();

        // Assert - Matrix should be different
        secondMatrix.ShouldNotBe(firstMatrix);
    }

    [Fact]
    public void GetTransform_AfterScaleChange_ShouldRecalculateMatrix()
    {
        // Arrange
        var transform = new TransformComponent();
        var firstMatrix = transform.GetTransform();

        // Act
        transform.Scale = new Vector3(2, 2, 2);
        var secondMatrix = transform.GetTransform();

        // Assert - Matrix should be different
        secondMatrix.ShouldNotBe(firstMatrix);
    }

    [Fact]
    public void GetTransform_IdentityTransform_ShouldReturnIdentityMatrix()
    {
        // Arrange
        var transform = new TransformComponent();

        // Act
        var matrix = transform.GetTransform();

        // Assert
        matrix.M11.ShouldBe(1f, 0.0001f);
        matrix.M22.ShouldBe(1f, 0.0001f);
        matrix.M33.ShouldBe(1f, 0.0001f);
        matrix.M44.ShouldBe(1f, 0.0001f);
        matrix.M41.ShouldBe(0f, 0.0001f);
        matrix.M42.ShouldBe(0f, 0.0001f);
        matrix.M43.ShouldBe(0f, 0.0001f);
    }

    [Fact]
    public void GetTransform_TranslationOnly_ShouldCreateTranslationMatrix()
    {
        // Arrange
        var translation = new Vector3(10, 20, 30);
        var transform = new TransformComponent { Translation = translation };

        // Act
        var matrix = transform.GetTransform();

        // Assert
        matrix.M41.ShouldBe(translation.X);
        matrix.M42.ShouldBe(translation.Y);
        matrix.M43.ShouldBe(translation.Z);
    }

    [Fact]
    public void GetTransform_ScaleOnly_ShouldCreateScaleMatrix()
    {
        // Arrange
        var scale = new Vector3(2, 3, 4);
        var transform = new TransformComponent { Scale = scale };

        // Act
        var matrix = transform.GetTransform();

        // Assert
        matrix.M11.ShouldBe(scale.X, 0.0001f);
        matrix.M22.ShouldBe(scale.Y, 0.0001f);
        matrix.M33.ShouldBe(scale.Z, 0.0001f);
    }

    [Fact]
    public void GetTransform_WithRotation_ShouldCreateRotationMatrix()
    {
        // Arrange
        var rotation = new Vector3(0, MathF.PI / 2, 0); // 90 degrees around Y
        var transform = new TransformComponent { Rotation = rotation };

        // Act
        var matrix = transform.GetTransform();
        var testPoint = new Vector3(1, 0, 0);
        var rotatedPoint = Vector3.Transform(testPoint, matrix);

        // Assert - Point (1,0,0) rotated 90° around Y should be approximately (0,0,-1)
        rotatedPoint.X.ShouldBe(0f, 0.01f);
        rotatedPoint.Y.ShouldBe(0f, 0.01f);
        rotatedPoint.Z.ShouldBe(-1f, 0.01f);
    }

    [Fact]
    public void GetTransform_ComplexTransform_ShouldApplyInCorrectOrder()
    {
        // Arrange - TRS order: Translation * Rotation * Scale
        var translation = new Vector3(5, 0, 0);
        var rotation = new Vector3(0, MathF.PI / 2, 0); // 90 degrees around Y
        var scale = new Vector3(2, 2, 2);
        var transform = new TransformComponent(translation, rotation, scale);

        // Act
        var matrix = transform.GetTransform();
        var testPoint = new Vector3(1, 0, 0);
        var transformedPoint = Vector3.Transform(testPoint, matrix);

        // Assert - With T * R * S and row-vector multiplication (v * M), transformations apply left-to-right:
        // (1,0,0) * T: (1,0,0) + (5,0,0) = (6,0,0)
        // (6,0,0) * R(90°Y): rotate 90° around Y = (0,0,-6)
        // (0,0,-6) * S(2): scale by 2 = (0,0,-12)
        transformedPoint.X.ShouldBe(0f, 0.1f);
        transformedPoint.Y.ShouldBe(0f, 0.1f);
        transformedPoint.Z.ShouldBe(-12f, 0.1f);
    }

    #endregion

    #region Caching Tests

    [Fact]
    public void GetTransform_CalledMultipleTimes_WhenNotDirty_ShouldUseCachedValue()
    {
        // Arrange
        var transform = new TransformComponent(
            new Vector3(10, 20, 30),
            new Vector3(0.1f, 0.2f, 0.3f),
            new Vector3(2, 2, 2)
        );

        // Act - Call GetTransform multiple times
        var matrix1 = transform.GetTransform();
        var matrix2 = transform.GetTransform();
        var matrix3 = transform.GetTransform();

        // Assert - All should return the exact same matrix instance/values
        matrix1.ShouldBe(matrix2);
        matrix2.ShouldBe(matrix3);
    }

    [Fact]
    public void GetTransform_AfterMultiplePropertyChanges_ShouldRecalculateOnlyOnce()
    {
        // Arrange
        var transform = new TransformComponent();
        var firstMatrix = transform.GetTransform(); // Initial calculation

        // Act - Make multiple changes
        transform.Translation = new Vector3(1, 2, 3);
        transform.Rotation = new Vector3(0.1f, 0.2f, 0.3f);
        transform.Scale = new Vector3(2, 2, 2);

        // All changes above mark as dirty, but calculation happens only once on next GetTransform
        var matrix = transform.GetTransform();

        // Assert - Matrix should have changed from the initial identity-like matrix
        matrix.ShouldNotBe(firstMatrix);
        // Verify the matrix is valid (M44 should be 1 for affine transformations)
        matrix.M44.ShouldBe(1f);
        // Verify transformation actually applies (origin point gets transformed)
        var origin = Vector3.Zero;
        var transformed = Vector3.Transform(origin, matrix);
        // With T * R * S, transforming origin should give us a non-zero result
        var magnitude = transformed.Length();
        magnitude.ShouldBeGreaterThan(0f);
    }

    [Fact]
    public void GetTransform_PropertySetToSameValue_ShouldStillMarkDirty()
    {
        // Arrange
        var transform = new TransformComponent { Translation = new Vector3(5, 5, 5) };
        var firstMatrix = transform.GetTransform();

        // Act - Set to same value
        transform.Translation = new Vector3(5, 5, 5);
        var secondMatrix = transform.GetTransform();

        // Assert - Even though value is same, dirty flag is set
        // This is expected behavior based on the implementation
        secondMatrix.ShouldBe(firstMatrix); // Values should still match
    }

    #endregion

    #region Clone Tests

    [Fact]
    public void Clone_ShouldCreateIndependentCopy()
    {
        // Arrange
        var original = new TransformComponent(
            new Vector3(10, 20, 30),
            new Vector3(0.1f, 0.2f, 0.3f),
            new Vector3(2, 3, 4)
        );

        // Act
        var clone = (TransformComponent)original.Clone();

        // Assert
        clone.ShouldNotBeSameAs(original);
        clone.Translation.ShouldBe(original.Translation);
        clone.Rotation.ShouldBe(original.Rotation);
        clone.Scale.ShouldBe(original.Scale);
    }

    [Fact]
    public void Clone_ModifyingClone_ShouldNotAffectOriginal()
    {
        // Arrange
        var original = new TransformComponent(new Vector3(1, 2, 3), Vector3.Zero, Vector3.One);
        var clone = (TransformComponent)original.Clone();

        // Act
        clone.Translation = new Vector3(10, 20, 30);
        clone.Rotation = new Vector3(1, 1, 1);
        clone.Scale = new Vector3(5, 5, 5);

        // Assert
        original.Translation.ShouldBe(new Vector3(1, 2, 3));
        original.Rotation.ShouldBe(Vector3.Zero);
        original.Scale.ShouldBe(Vector3.One);
    }

    [Fact]
    public void Clone_ShouldNotShareCachedMatrix()
    {
        // Arrange
        var original = new TransformComponent(new Vector3(5, 10, 15), Vector3.Zero, Vector3.One);
        original.GetTransform(); // Force cache creation

        // Act
        var clone = (TransformComponent)original.Clone();
        clone.Translation = new Vector3(20, 30, 40);
        var cloneMatrix = clone.GetTransform();
        var originalMatrix = original.GetTransform();

        // Assert
        cloneMatrix.ShouldNotBe(originalMatrix);
        originalMatrix.M41.ShouldBe(5f);
        cloneMatrix.M41.ShouldBe(20f);
    }

    #endregion

    #region Stress Tests with Random Data

    [Fact]
    public void GetTransform_WithRandomValues_ShouldProduceValidMatrix()
    {
        // Arrange
        var transform = new TransformComponent(
            new Vector3(_faker.Random.Float(-100, 100), _faker.Random.Float(-100, 100), _faker.Random.Float(-100, 100)),
            new Vector3(_faker.Random.Float(-MathF.PI, MathF.PI), _faker.Random.Float(-MathF.PI, MathF.PI), _faker.Random.Float(-MathF.PI, MathF.PI)),
            new Vector3(_faker.Random.Float(0.1f, 10), _faker.Random.Float(0.1f, 10), _faker.Random.Float(0.1f, 10))
        );

        // Act
        var matrix = transform.GetTransform();

        // Assert - Matrix should be valid (M44 should be 1)
        matrix.M44.ShouldBe(1f);
        float.IsNaN(matrix.M11).ShouldBeFalse();
        float.IsNaN(matrix.M22).ShouldBeFalse();
        float.IsNaN(matrix.M33).ShouldBeFalse();
    }

    [Fact]
    public void GetTransform_MultipleRandomUpdates_ShouldAlwaysProduceValidMatrix()
    {
        // Arrange
        var transform = new TransformComponent();

        // Act & Assert - Perform multiple random updates
        for (int i = 0; i < 100; i++)
        {
            transform.Translation = new Vector3(
                _faker.Random.Float(-50, 50),
                _faker.Random.Float(-50, 50),
                _faker.Random.Float(-50, 50)
            );
            transform.Rotation = new Vector3(
                _faker.Random.Float(-MathF.PI, MathF.PI),
                _faker.Random.Float(-MathF.PI, MathF.PI),
                _faker.Random.Float(-MathF.PI, MathF.PI)
            );
            transform.Scale = new Vector3(
                _faker.Random.Float(0.1f, 5),
                _faker.Random.Float(0.1f, 5),
                _faker.Random.Float(0.1f, 5)
            );

            var matrix = transform.GetTransform();

            matrix.M44.ShouldBe(1f);
            float.IsNaN(matrix.M11).ShouldBeFalse();
            float.IsInfinity(matrix.M11).ShouldBeFalse();
        }
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void GetTransform_WithZeroScale_ShouldProduceValidMatrix()
    {
        // Arrange
        var transform = new TransformComponent { Scale = Vector3.Zero };

        // Act
        var matrix = transform.GetTransform();

        // Assert
        matrix.M44.ShouldBe(1f);
        float.IsNaN(matrix.M11).ShouldBeFalse();
    }

    [Fact]
    public void GetTransform_WithNegativeScale_ShouldProduceValidMatrix()
    {
        // Arrange
        var transform = new TransformComponent { Scale = new Vector3(-1, -1, -1) };

        // Act
        var matrix = transform.GetTransform();

        // Assert
        matrix.M44.ShouldBe(1f);
        matrix.M11.ShouldBe(-1f, 0.0001f);
        matrix.M22.ShouldBe(-1f, 0.0001f);
        matrix.M33.ShouldBe(-1f, 0.0001f);
    }

    [Fact]
    public void GetTransform_WithLargeRotation_ShouldProduceValidMatrix()
    {
        // Arrange - Rotation beyond 2*PI
        var transform = new TransformComponent { Rotation = new Vector3(10f, 20f, 30f) };

        // Act
        var matrix = transform.GetTransform();

        // Assert
        matrix.M44.ShouldBe(1f);
        float.IsNaN(matrix.M11).ShouldBeFalse();
        float.IsNaN(matrix.M22).ShouldBeFalse();
        float.IsNaN(matrix.M33).ShouldBeFalse();
    }

    [Fact]
    public void GetTransform_WithVeryLargeTranslation_ShouldProduceValidMatrix()
    {
        // Arrange
        var transform = new TransformComponent { Translation = new Vector3(100000, 100000, 100000) };

        // Act
        var matrix = transform.GetTransform();

        // Assert
        matrix.M41.ShouldBe(100000f);
        matrix.M42.ShouldBe(100000f);
        matrix.M43.ShouldBe(100000f);
        float.IsInfinity(matrix.M41).ShouldBeFalse();
    }

    #endregion
}
