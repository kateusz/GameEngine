using System.Numerics;
using Engine.Scene.Components;
using Shouldly;

namespace Engine.Tests.Components;

public class BoxCollider2DComponentTests
{
    [Fact]
    public void BoxCollider2DComponent_DefaultConstructor_ShouldInitializeWithDefaults()
    {
        // Act
        var component = new BoxCollider2DComponent();

        // Assert
        component.Size.ShouldBe(Vector2.Zero);
        component.Offset.ShouldBe(Vector2.Zero);
        component.Density.ShouldBe(1.0f);
        component.Friction.ShouldBe(0.5f);
        component.Restitution.ShouldBe(0.0f);
        component.RestitutionThreshold.ShouldBe(0.5f);
        component.IsTrigger.ShouldBeFalse();
        component.IsDirty.ShouldBeTrue(); // Initially dirty
    }

    [Fact]
    public void BoxCollider2DComponent_ParameterizedConstructor_ShouldSetAllProperties()
    {
        // Arrange
        var size = new Vector2(2f, 3f);
        var offset = new Vector2(0.5f, 0.5f);

        // Act
        var component = new BoxCollider2DComponent(size, offset, 2.0f, 0.3f, 0.8f, 1.0f, true);

        // Assert
        component.Size.ShouldBe(size);
        component.Offset.ShouldBe(offset);
        component.Density.ShouldBe(2.0f);
        component.Friction.ShouldBe(0.3f);
        component.Restitution.ShouldBe(0.8f);
        component.RestitutionThreshold.ShouldBe(1.0f);
        component.IsTrigger.ShouldBeTrue();
        component.IsDirty.ShouldBeTrue(); // Initially dirty
    }

    [Fact]
    public void BoxCollider2DComponent_SetDensity_ShouldMarkAsDirty()
    {
        // Arrange
        var component = new BoxCollider2DComponent();
        component.ClearDirtyFlag();
        component.IsDirty.ShouldBeFalse();

        // Act
        component.Density = 5.0f;

        // Assert
        component.Density.ShouldBe(5.0f);
        component.IsDirty.ShouldBeTrue();
    }

    [Fact]
    public void BoxCollider2DComponent_SetDensity_ToSameValue_ShouldNotMarkDirty()
    {
        // Arrange
        var component = new BoxCollider2DComponent();
        component.Density = 3.0f;
        component.ClearDirtyFlag();

        // Act
        component.Density = 3.0f; // Same value

        // Assert
        component.IsDirty.ShouldBeFalse();
    }

    [Fact]
    public void BoxCollider2DComponent_SetFriction_ShouldMarkAsDirty()
    {
        // Arrange
        var component = new BoxCollider2DComponent();
        component.ClearDirtyFlag();

        // Act
        component.Friction = 0.8f;

        // Assert
        component.Friction.ShouldBe(0.8f);
        component.IsDirty.ShouldBeTrue();
    }

    [Fact]
    public void BoxCollider2DComponent_SetRestitution_ShouldMarkAsDirty()
    {
        // Arrange
        var component = new BoxCollider2DComponent();
        component.ClearDirtyFlag();

        // Act
        component.Restitution = 0.9f;

        // Assert
        component.Restitution.ShouldBe(0.9f);
        component.IsDirty.ShouldBeTrue();
    }

    [Fact]
    public void BoxCollider2DComponent_ClearDirtyFlag_ShouldResetFlag()
    {
        // Arrange
        var component = new BoxCollider2DComponent();
        component.Density = 2.0f;
        component.IsDirty.ShouldBeTrue();

        // Act
        component.ClearDirtyFlag();

        // Assert
        component.IsDirty.ShouldBeFalse();
    }

    [Fact]
    public void BoxCollider2DComponent_MultiplePropertyChanges_ShouldStayDirty()
    {
        // Arrange
        var component = new BoxCollider2DComponent();
        component.ClearDirtyFlag();

        // Act
        component.Density = 2.0f;
        component.Friction = 0.7f;
        component.Restitution = 0.5f;

        // Assert
        component.IsDirty.ShouldBeTrue();
    }

    [Fact]
    public void BoxCollider2DComponent_Clone_ShouldCopyAllProperties()
    {
        // Arrange
        var original = new BoxCollider2DComponent(
            new Vector2(10f, 10f),
            new Vector2(1f, 1f),
            3.0f, 0.6f, 0.4f, 0.8f, true);

        // Act
        var clone = (BoxCollider2DComponent)original.Clone();

        // Assert
        clone.ShouldNotBeSameAs(original);
        clone.Size.ShouldBe(original.Size);
        clone.Offset.ShouldBe(original.Offset);
        clone.Density.ShouldBe(original.Density);
        clone.Friction.ShouldBe(original.Friction);
        clone.Restitution.ShouldBe(original.Restitution);
        clone.RestitutionThreshold.ShouldBe(original.RestitutionThreshold);
        clone.IsTrigger.ShouldBe(original.IsTrigger);
    }
}