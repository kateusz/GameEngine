using SceneComponents.Camera;
using Shouldly;

namespace Engine.Tests.Components;

public class CameraComponentTests
{
    [Fact]
    public void CameraComponent_DefaultConstructor_ShouldInitializeWithDefaults()
    {
        // Act
        var component = new CameraComponent();

        // Assert
        component.ProjectionType.ShouldBe(CameraProjectionTypeData.Orthographic);
        component.Primary.ShouldBeFalse();
        component.FixedAspectRatio.ShouldBeFalse();
    }

    [Fact]
    public void CameraComponent_SetPrimary_ShouldUpdateValue()
    {
        // Arrange
        var component = new CameraComponent();

        // Act
        component.Primary = false;

        // Assert
        component.Primary.ShouldBeFalse();
    }

    [Fact]
    public void CameraComponent_SetFixedAspectRatio_ShouldUpdateValue()
    {
        // Arrange
        var component = new CameraComponent();

        // Act
        component.FixedAspectRatio = true;

        // Assert
        component.FixedAspectRatio.ShouldBeTrue();
    }

    [Fact]
    public void CameraComponent_Clone_ShouldCopyAllCameraProperties()
    {
        // Arrange
        var original = new CameraComponent
        {
            Primary = false,
            FixedAspectRatio = true,
            ProjectionType = CameraProjectionTypeData.Orthographic,
            OrthographicSize = 15f,
            OrthographicNear = -2f,
            OrthographicFar = 2f,
            AspectRatio = 1920f / 1080f
        };

        // Act
        var clone = (CameraComponent)original.Clone();

        // Assert
        clone.ShouldNotBeSameAs(original);
        clone.Primary.ShouldBe(false);
        clone.FixedAspectRatio.ShouldBeTrue();
        clone.ProjectionType.ShouldBe(original.ProjectionType);
        clone.OrthographicSize.ShouldBe(original.OrthographicSize);
        clone.OrthographicNear.ShouldBe(original.OrthographicNear);
        clone.OrthographicFar.ShouldBe(original.OrthographicFar);
        clone.AspectRatio.ShouldBe(original.AspectRatio);
    }

    [Fact]
    public void CameraComponent_Clone_ShouldCopyPerspectiveProperties()
    {
        // Arrange
        var original = new CameraComponent
        {
            ProjectionType = CameraProjectionTypeData.Perspective,
            PerspectiveFOV = MathF.PI / 3,
            PerspectiveNear = 0.5f,
            PerspectiveFar = 2000f
        };

        // Act
        var clone = (CameraComponent)original.Clone();

        // Assert
        clone.ProjectionType.ShouldBe(CameraProjectionTypeData.Perspective);
        clone.PerspectiveFOV.ShouldBe(original.PerspectiveFOV);
        clone.PerspectiveNear.ShouldBe(original.PerspectiveNear);
        clone.PerspectiveFar.ShouldBe(original.PerspectiveFar);
    }

    [Fact]
    public void CameraComponent_Clone_ModifyingClone_ShouldNotAffectOriginal()
    {
        // Arrange
        var original = new CameraComponent
        {
            ProjectionType = CameraProjectionTypeData.Orthographic,
            OrthographicSize = 10f,
            OrthographicNear = -1f,
            OrthographicFar = 1f
        };

        // Act
        var clone = (CameraComponent)original.Clone();
        clone.OrthographicSize = 20f;
        clone.OrthographicNear = -2f;
        clone.OrthographicFar = 2f;

        // Assert
        original.OrthographicSize.ShouldBe(10f);
        clone.OrthographicSize.ShouldBe(20f);
    }
}