using Engine.Scene.Components;
using Shouldly;

namespace Engine.Tests.Components;

public class RigidBody2DComponentTests
{
    [Fact]
    public void RigidBody2DComponent_DefaultConstructor_ShouldInitializeWithDefaults()
    {
        // Act
        var component = new RigidBody2DComponent();

        // Assert
        component.BodyType.ShouldBe(RigidBodyType.Static);
        component.FixedRotation.ShouldBeFalse();
        component.RuntimeBody.ShouldBeNull();
    }

    [Fact]
    public void RigidBody2DComponent_SetBodyType_ShouldUpdateValue()
    {
        // Arrange
        var component = new RigidBody2DComponent();

        // Act
        component.BodyType = RigidBodyType.Dynamic;

        // Assert
        component.BodyType.ShouldBe(RigidBodyType.Dynamic);
    }

    [Fact]
    public void RigidBody2DComponent_SetFixedRotation_ShouldUpdateValue()
    {
        // Arrange
        var component = new RigidBody2DComponent();

        // Act
        component.FixedRotation = true;

        // Assert
        component.FixedRotation.ShouldBeTrue();
    }

    [Fact]
    public void RigidBody2DComponent_Clone_ShouldCopyProperties_WithoutRuntimeBody()
    {
        // Arrange
        var original = new RigidBody2DComponent
        {
            BodyType = RigidBodyType.Kinematic,
            FixedRotation = true,
            RuntimeBody = null // Would be a Box2D body at runtime
        };

        // Act
        var clone = (RigidBody2DComponent)original.Clone();

        // Assert
        clone.ShouldNotBeSameAs(original);
        clone.BodyType.ShouldBe(RigidBodyType.Kinematic);
        clone.FixedRotation.ShouldBeTrue();
        clone.RuntimeBody.ShouldBeNull(); // Should not clone runtime body
    }

    [Theory]
    [InlineData(RigidBodyType.Static)]
    [InlineData(RigidBodyType.Dynamic)]
    [InlineData(RigidBodyType.Kinematic)]
    public void RigidBody2DComponent_AllBodyTypes_ShouldBeSettable(RigidBodyType bodyType)
    {
        // Arrange
        var component = new RigidBody2DComponent();

        // Act
        component.BodyType = bodyType;

        // Assert
        component.BodyType.ShouldBe(bodyType);
    }
}