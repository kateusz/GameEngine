using System.Numerics;
using Engine.Renderer.Cameras;
using Shouldly;

namespace Engine.Tests.Cameras;

public class OrthographicCameraTests
{
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
}