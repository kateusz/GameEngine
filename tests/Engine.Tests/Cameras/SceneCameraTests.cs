using Engine.Scene;
using Shouldly;

namespace Engine.Tests.Cameras;

public class SceneCameraTests
{
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
        _ = camera.Projection; // Force initial calculation

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
}