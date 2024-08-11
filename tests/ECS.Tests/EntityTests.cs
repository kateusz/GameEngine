using FluentAssertions;

namespace ECS.Tests;

public class EntityTests
{
    private class TransformComponent : Component;

    private class VelocityComponent : Component;

    [Fact]
    public void Entity_Should_Have_Unique_Id()
    {
        // Act
        var entity1 = new Entity("Entity1");
        var entity2 = new Entity("Entity2");

        // Assert
        entity1.Id.Should().NotBe(entity2.Id);
    }

    [Fact]
    public void AddComponent_Should_Add_TransformComponent_To_Entity()
    {
        // Arrange
        var entity = new Entity("Entity1");
        var transformComponent = new TransformComponent();

        // Act
        entity.AddComponent(transformComponent);

        // Assert
        entity.HasComponent<TransformComponent>().Should().BeTrue();
        entity.GetComponent<TransformComponent>().Should().Be(transformComponent);
    }

    [Fact]
    public void AddComponent_Generic_Should_Add_TransformComponent_To_Entity()
    {
        // Arrange
        var entity = new Entity("Entity1");

        // Act
        entity.AddComponent<TransformComponent>();

        // Assert
        entity.HasComponent<TransformComponent>().Should().BeTrue();
    }

    [Fact]
    public void RemoveComponent_Should_Remove_TransformComponent_From_Entity()
    {
        // Arrange
        var entity = new Entity("Entity1");
        entity.AddComponent<TransformComponent>();

        // Act
        entity.RemoveComponent<TransformComponent>();

        // Assert
        entity.HasComponent<TransformComponent>().Should().BeFalse();
    }

    [Fact]
    public void HasComponents_Should_Return_True_If_All_Components_Exist()
    {
        // Arrange
        var entity = new Entity("Entity1");
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<VelocityComponent>();

        // Act
        var hasComponents = entity.HasComponents(new[] { typeof(TransformComponent), typeof(VelocityComponent) });

        // Assert
        hasComponents.Should().BeTrue();
    }

    [Fact]
    public void HasComponents_Should_Return_False_If_Any_Component_Is_Missing()
    {
        // Arrange
        var entity = new Entity("Entity1");
        entity.AddComponent<TransformComponent>();

        // Act
        var hasComponents = entity.HasComponents(new[] { typeof(TransformComponent), typeof(VelocityComponent) });

        // Assert
        hasComponents.Should().BeFalse();
    }
}