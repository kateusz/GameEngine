using Bogus;
using Engine.Scene.Components;
using Shouldly;

namespace Engine.Tests.Components;

public class IdComponentTests
{
    private readonly Faker _faker = new();

    [Fact]
    public void IdComponent_DefaultConstructor_ShouldInitializeWithZero()
    {
        // Act
        var component = new IdComponent();

        // Assert
        component.Id.ShouldBe(0);
    }

    [Fact]
    public void IdComponent_ParameterizedConstructor_ShouldSetId()
    {
        // Arrange
        var id = _faker.Random.Long(1, 1000000);

        // Act
        var component = new IdComponent(id);

        // Assert
        component.Id.ShouldBe(id);
    }

    [Fact]
    public void IdComponent_Clone_ShouldCopyId()
    {
        // Arrange
        var original = new IdComponent(12345);

        // Act
        var clone = (IdComponent)original.Clone();

        // Assert
        clone.Id.ShouldBe(original.Id);
        clone.ShouldNotBeSameAs(original);
    }

    [Fact]
    public void IdComponent_SetId_ShouldUpdateValue()
    {
        // Arrange
        var component = new IdComponent();

        // Act
        component.Id = 99999;

        // Assert
        component.Id.ShouldBe(99999);
    }
}