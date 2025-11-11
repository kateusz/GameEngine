using Engine.Scene;
using Engine.Scene.Components;
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
        component.Camera.ShouldNotBeNull();
        component.Primary.ShouldBeTrue();
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
            FixedAspectRatio = true
        };
        original.Camera.SetOrthographic(15f, -2f, 2f);
        original.Camera.SetViewportSize(1920, 1080);

        // Act
        var clone = (CameraComponent)original.Clone();

        // Assert
        clone.ShouldNotBeSameAs(original);
        clone.Camera.ShouldNotBeSameAs(original.Camera);
        clone.Primary.ShouldBe(false);
        clone.FixedAspectRatio.ShouldBeTrue();
        clone.Camera.ProjectionType.ShouldBe(original.Camera.ProjectionType);
        clone.Camera.OrthographicSize.ShouldBe(original.Camera.OrthographicSize);
        clone.Camera.OrthographicNear.ShouldBe(original.Camera.OrthographicNear);
        clone.Camera.OrthographicFar.ShouldBe(original.Camera.OrthographicFar);
        clone.Camera.AspectRatio.ShouldBe(original.Camera.AspectRatio);
    }

    [Fact]
    public void CameraComponent_Clone_ShouldCopyPerspectiveProperties()
    {
        // Arrange
        var original = new CameraComponent();
        original.Camera.SetPerspective(MathF.PI / 3, 0.5f, 2000f);

        // Act
        var clone = (CameraComponent)original.Clone();

        // Assert
        clone.Camera.ProjectionType.ShouldBe(ProjectionType.Perspective);
        clone.Camera.PerspectiveFOV.ShouldBe(original.Camera.PerspectiveFOV);
        clone.Camera.PerspectiveNear.ShouldBe(original.Camera.PerspectiveNear);
        clone.Camera.PerspectiveFar.ShouldBe(original.Camera.PerspectiveFar);
    }

    [Fact]
    public void CameraComponent_Clone_ModifyingClone_ShouldNotAffectOriginal()
    {
        // Arrange
        var original = new CameraComponent();
        original.Camera.SetOrthographic(10f, -1f, 1f);

        // Act
        var clone = (CameraComponent)original.Clone();
        clone.Camera.SetOrthographic(20f, -2f, 2f);

        // Assert
        original.Camera.OrthographicSize.ShouldBe(10f);
        clone.Camera.OrthographicSize.ShouldBe(20f);
    }
}