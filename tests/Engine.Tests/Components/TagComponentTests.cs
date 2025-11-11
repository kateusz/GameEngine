using Bogus;
using Engine.Scene.Components;
using Shouldly;

namespace Engine.Tests.Components;

public class TagComponentTests
{
    private readonly Faker _faker = new();
    
    [Fact]
    public void TagComponent_DefaultConstructor_ShouldInitializeWithEmptyString()
    {
        // Act
        var component = new TagComponent();

        // Assert
        component.Tag.ShouldBe(string.Empty);
    }

    [Fact]
    public void TagComponent_ParameterizedConstructor_ShouldSetTag()
    {
        // Arrange
        var tag = _faker.Lorem.Word();

        // Act
        var component = new TagComponent(tag);

        // Assert
        component.Tag.ShouldBe(tag);
    }

    [Fact]
    public void TagComponent_Clone_ShouldCreateIndependentCopy()
    {
        // Arrange
        var original = new TagComponent("original-tag");

        // Act
        var clone = (TagComponent)original.Clone();
        clone.Tag = "modified-tag";

        // Assert
        clone.ShouldNotBeSameAs(original);
        original.Tag.ShouldBe("original-tag");
        clone.Tag.ShouldBe("modified-tag");
    }
}