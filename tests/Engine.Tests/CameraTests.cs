using System.Numerics;
using Bogus;
using Engine.Renderer.Cameras;
using Engine.Scene;
using Shouldly;
using Xunit;

namespace Engine.Tests;

public class CameraTests
{
    private readonly Faker _faker = new();

    #region OrthographicCamera Tests

    [Fact]
    public void OrthographicCamera_Constructor_ShouldInitializeWithGivenBounds()
    {
        // Arrange & Act
        var camera = new OrthographicCamera(-10f, 10f, -5f, 5f);

        // Assert
        camera.Position.ShouldBe(Vector3.Zero);
        camera.Rotation.ShouldBe(0f);
        camera.Scale.ShouldBe(Vector3.One);
        camera.ProjectionMatrix.ShouldNotBe(Matrix4x4.Identity);
    }

    [Fact]
    public void OrthographicCamera_SetPosition_ShouldUpdatePosition()
    {
        // Arrange
        var camera = new OrthographicCamera(-1f, 1f, -1f, 1f);
        var newPosition = new Vector3(5f, 10f, 0f);

        // Act
        camera.SetPosition(newPosition);

        // Assert
        camera.Position.ShouldBe(newPosition);
    }

    [Fact]
    public void OrthographicCamera_SetPosition_ShouldRecalculateViewMatrix()
    {
        // Arrange
        var camera = new OrthographicCamera(-1f, 1f, -1f, 1f);
        var initialViewMatrix = camera.ViewMatrix;

        // Act
        camera.SetPosition(new Vector3(5f, 5f, 0f));

        // Assert
        camera.ViewMatrix.ShouldNotBe(initialViewMatrix);
    }

    [Fact]
    public void OrthographicCamera_SetRotation_ShouldUpdateRotation()
    {
        // Arrange
        var camera = new OrthographicCamera(-1f, 1f, -1f, 1f);
        var rotation = 45f;

        // Act
        camera.SetRotation(rotation);

        // Assert
        camera.Rotation.ShouldBe(rotation);
    }

    [Fact]
    public void OrthographicCamera_SetRotation_ShouldRecalculateViewMatrix()
    {
        // Arrange
        var camera = new OrthographicCamera(-1f, 1f, -1f, 1f);
        var initialViewMatrix = camera.ViewMatrix;

        // Act
        camera.SetRotation(90f);

        // Assert
        camera.ViewMatrix.ShouldNotBe(initialViewMatrix);
    }

    [Fact]
    public void OrthographicCamera_SetScale_ShouldUpdateScale()
    {
        // Arrange
        var camera = new OrthographicCamera(-1f, 1f, -1f, 1f);
        var newScale = new Vector3(2f, 2f, 1f);

        // Act
        camera.SetScale(newScale);

        // Assert
        camera.Scale.ShouldBe(newScale);
    }

    [Fact]
    public void OrthographicCamera_SetScale_ShouldRecalculateViewMatrix()
    {
        // Arrange
        var camera = new OrthographicCamera(-1f, 1f, -1f, 1f);
        var initialViewMatrix = camera.ViewMatrix;

        // Act
        camera.SetScale(new Vector3(2f, 2f, 1f));

        // Assert
        camera.ViewMatrix.ShouldNotBe(initialViewMatrix);
    }

    [Fact]
    public void OrthographicCamera_SetProjection_ShouldUpdateProjectionMatrix()
    {
        // Arrange
        var camera = new OrthographicCamera(-1f, 1f, -1f, 1f);
        var initialProjection = camera.ProjectionMatrix;

        // Act
        camera.SetProjection(-5f, 5f, -5f, 5f);

        // Assert
        camera.ProjectionMatrix.ShouldNotBe(initialProjection);
    }

    [Fact]
    public void OrthographicCamera_SetProjection_ShouldUpdateViewProjectionMatrix()
    {
        // Arrange
        var camera = new OrthographicCamera(-1f, 1f, -1f, 1f);
        var initialVP = camera.ViewProjectionMatrix;

        // Act
        camera.SetProjection(-10f, 10f, -10f, 10f);

        // Assert
        camera.ViewProjectionMatrix.ShouldNotBe(initialVP);
    }

    [Fact]
    public void OrthographicCamera_ViewProjectionMatrix_ShouldBeProductOfProjectionAndView()
    {
        // Arrange
        var camera = new OrthographicCamera(-5f, 5f, -5f, 5f);
        camera.SetPosition(new Vector3(2f, 3f, 0f));
        camera.SetRotation(45f);

        // Act
        var expected = camera.ProjectionMatrix * camera.ViewMatrix;

        // Assert
        camera.ViewProjectionMatrix.M11.ShouldBe(expected.M11, 0.0001f);
        camera.ViewProjectionMatrix.M12.ShouldBe(expected.M12, 0.0001f);
        camera.ViewProjectionMatrix.M13.ShouldBe(expected.M13, 0.0001f);
        camera.ViewProjectionMatrix.M14.ShouldBe(expected.M14, 0.0001f);
    }

    [Fact]
    public void OrthographicCamera_MultipleTransformations_ShouldMaintainConsistency()
    {
        // Arrange
        var camera = new OrthographicCamera(-10f, 10f, -10f, 10f);

        // Act
        camera.SetPosition(new Vector3(5f, 5f, 0f));
        camera.SetRotation(30f);
        camera.SetScale(new Vector3(1.5f, 1.5f, 1f));

        // Assert
        camera.Position.ShouldBe(new Vector3(5f, 5f, 0f));
        camera.Rotation.ShouldBe(30f);
        camera.Scale.ShouldBe(new Vector3(1.5f, 1.5f, 1f));
        float.IsNaN(camera.ViewMatrix.M11).ShouldBeFalse();
        float.IsInfinity(camera.ViewMatrix.M11).ShouldBeFalse();
    }

    #endregion

    #region SceneCamera Tests

    [Fact]
    public void SceneCamera_Constructor_ShouldInitializeWithOrthographicProjection()
    {
        // Act
        var camera = new SceneCamera();

        // Assert
        camera.ProjectionType.ShouldBe(ProjectionType.Orthographic);
    }

    [Fact]
    public void SceneCamera_Constructor_ShouldMarkProjectionAsDirty()
    {
        // Arrange & Act
        var camera = new SceneCamera();

        // Access Projection to trigger calculation
        var projection = camera.Projection;

        // Assert - Should not throw and should have valid matrix
        projection.M44.ShouldBe(1f);
    }

    [Fact]
    public void SceneCamera_SetOrthographic_ShouldUpdateAllParameters()
    {
        // Arrange
        var camera = new SceneCamera();

        // Act
        camera.SetOrthographic(10f, -1f, 1f);

        // Assert
        camera.ProjectionType.ShouldBe(ProjectionType.Orthographic);
        camera.OrthographicSize.ShouldBe(10f);
        camera.OrthographicNear.ShouldBe(-1f);
        camera.OrthographicFar.ShouldBe(1f);
    }

    [Fact]
    public void SceneCamera_SetPerspective_ShouldUpdateAllParameters()
    {
        // Arrange
        var camera = new SceneCamera();

        // Act
        camera.SetPerspective(MathF.PI / 4, 0.1f, 1000f);

        // Assert
        camera.ProjectionType.ShouldBe(ProjectionType.Perspective);
        camera.PerspectiveFOV.ShouldBe(MathF.PI / 4);
        camera.PerspectiveNear.ShouldBe(0.1f);
        camera.PerspectiveFar.ShouldBe(1000f);
    }

    [Fact]
    public void SceneCamera_ChangeProjectionType_ShouldMarkProjectionDirty()
    {
        // Arrange
        var camera = new SceneCamera();
        camera.SetOrthographic(10f, -1f, 1f);
        var orthoProjection = camera.Projection;

        // Act
        camera.ProjectionType = ProjectionType.Perspective;
        var perspProjection = camera.Projection;

        // Assert
        perspProjection.ShouldNotBe(orthoProjection);
    }

    [Fact]
    public void SceneCamera_OrthographicSize_Set_ShouldMarkDirtyAndRecalculate()
    {
        // Arrange
        var camera = new SceneCamera();
        camera.SetOrthographic(10f, -1f, 1f);
        camera.SetViewportSize(800, 600);
        var initialProjection = camera.Projection;

        // Act
        camera.OrthographicSize = 20f;
        var newProjection = camera.Projection;

        // Assert
        newProjection.ShouldNotBe(initialProjection);
    }

    [Fact]
    public void SceneCamera_OrthographicSize_SetToSameValue_ShouldNotRecalculate()
    {
        // Arrange
        var camera = new SceneCamera();
        camera.OrthographicSize = 15f;
        var firstProjection = camera.Projection;

        // Act - Set to same value
        camera.OrthographicSize = 15f;
        var secondProjection = camera.Projection;

        // Assert
        secondProjection.ShouldBe(firstProjection);
    }

    [Fact]
    public void SceneCamera_AspectRatio_Set_ShouldMarkDirtyAndRecalculate()
    {
        // Arrange
        var camera = new SceneCamera();
        camera.SetOrthographic(10f, -1f, 1f);
        camera.AspectRatio = 1.0f;
        var initialProjection = camera.Projection;

        // Act
        camera.AspectRatio = 16f / 9f;
        var newProjection = camera.Projection;

        // Assert
        newProjection.ShouldNotBe(initialProjection);
    }

    [Fact]
    public void SceneCamera_SetViewportSize_ShouldCalculateCorrectAspectRatio()
    {
        // Arrange
        var camera = new SceneCamera();

        // Act
        camera.SetViewportSize(1920, 1080);

        // Assert
        camera.AspectRatio.ShouldBe(1920f / 1080f, 0.0001f);
    }

    [Fact]
    public void SceneCamera_SetViewportSize_WithZeroWidth_ShouldNotUpdate()
    {
        // Arrange
        var camera = new SceneCamera();
        camera.SetViewportSize(800, 600);
        var initialAspectRatio = camera.AspectRatio;

        // Act
        camera.SetViewportSize(0, 600);

        // Assert
        camera.AspectRatio.ShouldBe(initialAspectRatio);
    }

    [Fact]
    public void SceneCamera_SetViewportSize_WithZeroHeight_ShouldNotUpdate()
    {
        // Arrange
        var camera = new SceneCamera();
        camera.SetViewportSize(800, 600);
        var initialAspectRatio = camera.AspectRatio;

        // Act
        camera.SetViewportSize(800, 0);

        // Assert
        camera.AspectRatio.ShouldBe(initialAspectRatio);
    }

    [Fact]
    public void SceneCamera_PerspectiveFOV_Set_ShouldMarkDirtyAndRecalculate()
    {
        // Arrange
        var camera = new SceneCamera();
        camera.SetPerspective(MathF.PI / 4, 0.1f, 1000f);
        camera.SetViewportSize(800, 600);
        var initialProjection = camera.Projection;

        // Act
        camera.PerspectiveFOV = MathF.PI / 3;
        var newProjection = camera.Projection;

        // Assert
        newProjection.ShouldNotBe(initialProjection);
    }

    [Fact]
    public void SceneCamera_PerspectiveNear_Set_ShouldMarkDirtyAndRecalculate()
    {
        // Arrange
        var camera = new SceneCamera();
        camera.SetPerspective(MathF.PI / 4, 0.1f, 1000f);
        camera.SetViewportSize(800, 600);
        var initialProjection = camera.Projection;

        // Act
        camera.PerspectiveNear = 0.5f;
        var newProjection = camera.Projection;

        // Assert
        newProjection.ShouldNotBe(initialProjection);
    }

    [Fact]
    public void SceneCamera_PerspectiveFar_Set_ShouldMarkDirtyAndRecalculate()
    {
        // Arrange
        var camera = new SceneCamera();
        camera.SetPerspective(MathF.PI / 4, 0.1f, 1000f);
        camera.SetViewportSize(800, 600);
        var initialProjection = camera.Projection;

        // Act
        camera.PerspectiveFar = 5000f;
        var newProjection = camera.Projection;

        // Assert
        newProjection.ShouldNotBe(initialProjection);
    }

    [Fact]
    public void SceneCamera_Projection_WhenNotDirty_ShouldReturnCachedValue()
    {
        // Arrange
        var camera = new SceneCamera();
        camera.SetOrthographic(10f, -1f, 1f);
        camera.SetViewportSize(800, 600);

        // Act
        var projection1 = camera.Projection;
        var projection2 = camera.Projection;

        // Assert
        projection1.ShouldBe(projection2);
    }

    [Fact]
    public void SceneCamera_SetOrthographicSize_ShouldUpdateOrthographicSizeProperty()
    {
        // Arrange
        var camera = new SceneCamera();

        // Act
        camera.SetOrthographicSize(25f);

        // Assert
        camera.OrthographicSize.ShouldBe(25f);
    }

    [Fact]
    public void SceneCamera_OrthographicProjection_ShouldProduceValidMatrix()
    {
        // Arrange
        var camera = new SceneCamera();
        camera.SetOrthographic(10f, -1f, 1f);
        camera.SetViewportSize(1920, 1080);

        // Act
        var projection = camera.Projection;

        // Assert
        float.IsNaN(projection.M11).ShouldBeFalse();
        float.IsInfinity(projection.M11).ShouldBeFalse();
        projection.M44.ShouldBe(1f, 0.0001f);
    }

    [Fact]
    public void SceneCamera_PerspectiveProjection_ShouldProduceValidMatrix()
    {
        // Arrange
        var camera = new SceneCamera();
        camera.SetPerspective(MathF.PI / 4, 0.1f, 1000f);
        camera.SetViewportSize(1920, 1080);

        // Act
        var projection = camera.Projection;

        // Assert
        float.IsNaN(projection.M11).ShouldBeFalse();
        float.IsInfinity(projection.M11).ShouldBeFalse();
    }

    [Fact]
    public void SceneCamera_MultiplePropertyChanges_ShouldOnlyRecalculateOnAccess()
    {
        // Arrange
        var camera = new SceneCamera();
        camera.SetOrthographic(10f, -1f, 1f);
        camera.SetViewportSize(800, 600);
        camera.Projection; // Force initial calculation

        // Act - Make multiple changes but don't access Projection
        camera.OrthographicSize = 20f;
        camera.OrthographicNear = -2f;
        camera.OrthographicFar = 2f;
        camera.AspectRatio = 2.0f;

        // Only when we access Projection should recalculation happen
        var projection = camera.Projection;

        // Assert - All changes should be reflected
        projection.M44.ShouldBe(1f);
        float.IsNaN(projection.M11).ShouldBeFalse();
    }

    #endregion

    #region Edge Cases and Stress Tests

    [Fact]
    public void OrthographicCamera_WithZeroBounds_ShouldHandleGracefully()
    {
        // Arrange & Act
        var camera = new OrthographicCamera(0f, 0f, 0f, 0f);

        // Assert - Should not crash
        camera.ProjectionMatrix.ShouldNotBe(default(Matrix4x4));
    }

    [Fact]
    public void OrthographicCamera_WithNegativeBounds_ShouldWork()
    {
        // Arrange & Act
        var camera = new OrthographicCamera(-10f, -5f, -10f, -5f);

        // Assert
        float.IsNaN(camera.ProjectionMatrix.M11).ShouldBeFalse();
    }

    [Fact]
    public void OrthographicCamera_WithLargePosition_ShouldProduceValidMatrix()
    {
        // Arrange
        var camera = new OrthographicCamera(-1f, 1f, -1f, 1f);

        // Act
        camera.SetPosition(new Vector3(100000f, 100000f, 0f));

        // Assert
        float.IsInfinity(camera.ViewMatrix.M11).ShouldBeFalse();
        float.IsNaN(camera.ViewMatrix.M11).ShouldBeFalse();
    }

    [Fact]
    public void OrthographicCamera_WithLargeRotation_ShouldProduceValidMatrix()
    {
        // Arrange
        var camera = new OrthographicCamera(-1f, 1f, -1f, 1f);

        // Act
        camera.SetRotation(720f); // Multiple rotations

        // Assert
        float.IsNaN(camera.ViewMatrix.M11).ShouldBeFalse();
    }

    [Fact]
    public void SceneCamera_WithRandomOrthographicSettings_ShouldProduceValidMatrix()
    {
        // Arrange
        var camera = new SceneCamera();
        var faker = new Faker();

        // Act
        camera.SetOrthographic(
            faker.Random.Float(1f, 100f),
            faker.Random.Float(-10f, 0f),
            faker.Random.Float(0f, 10f)
        );
        camera.SetViewportSize(
            (uint)faker.Random.Int(100, 4000),
            (uint)faker.Random.Int(100, 4000)
        );

        var projection = camera.Projection;

        // Assert
        float.IsNaN(projection.M11).ShouldBeFalse();
        float.IsInfinity(projection.M11).ShouldBeFalse();
    }

    [Fact]
    public void SceneCamera_WithRandomPerspectiveSettings_ShouldProduceValidMatrix()
    {
        // Arrange
        var camera = new SceneCamera();
        var faker = new Faker();

        // Act
        camera.SetPerspective(
            faker.Random.Float(0.1f, MathF.PI / 2),
            faker.Random.Float(0.01f, 1f),
            faker.Random.Float(100f, 10000f)
        );
        camera.SetViewportSize(
            (uint)faker.Random.Int(100, 4000),
            (uint)faker.Random.Int(100, 4000)
        );

        var projection = camera.Projection;

        // Assert
        float.IsNaN(projection.M11).ShouldBeFalse();
        float.IsInfinity(projection.M11).ShouldBeFalse();
    }

    [Fact]
    public void SceneCamera_SwitchingProjectionTypes_MultipleTimes_ShouldMaintainConsistency()
    {
        // Arrange
        var camera = new SceneCamera();
        camera.SetViewportSize(800, 600);

        // Act & Assert - Switch multiple times
        for (int i = 0; i < 10; i++)
        {
            camera.SetOrthographic(10f, -1f, 1f);
            var orthoProjection = camera.Projection;
            float.IsNaN(orthoProjection.M11).ShouldBeFalse();

            camera.SetPerspective(MathF.PI / 4, 0.1f, 1000f);
            var perspProjection = camera.Projection;
            float.IsNaN(perspProjection.M11).ShouldBeFalse();
        }
    }

    #endregion
}
