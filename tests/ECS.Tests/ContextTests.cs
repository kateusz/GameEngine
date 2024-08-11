using FluentAssertions;

namespace ECS.Tests;

public class ContextTests
{
    private class PositionComponent : Component;
    private class VelocityComponent : Component;
    
    [Fact]
    public void Context_Instance_Should_Return_Same_Instance()
    {
        // Act
        var context1 = Context.Instance;
        var context2 = Context.Instance;

        // Assert
        context1.Should().BeSameAs(context2);
    }

    [Fact]
    public void Register_Should_Add_Entity_To_Context()
    {
        // Arrange
        var context = Context.Instance;
        var entity = new Entity("entity1");

        // Act
        context.Register(entity);

        // Assert
        context.Entities.Should().Contain(entity);
    }

    [Fact]
    public void GetGroup_Should_Return_Entities_With_Specific_Components()
    {
        // Arrange
        var context = Context.Instance;
        context.Entities.Clear(); // Clear the context for test isolation

        var entity1 = new Entity("entity1");
        entity1.AddComponent(new PositionComponent());

        var entity2 = new Entity("entity2");
        entity2.AddComponent(new PositionComponent());
        entity2.AddComponent(new VelocityComponent());

        context.Register(entity1);
        context.Register(entity2);

        // Act
        var group = context.GetGroup(typeof(PositionComponent));

        // Assert
        group.Should().HaveCount(2);
        group.Should().Contain(new[] { entity1, entity2 });
    }

    [Fact]
    public void View_Should_Return_Entities_With_Specific_Component_And_That_Component()
    {
        // Arrange
        var context = Context.Instance;
        context.Entities.Clear(); // Clear the context for test isolation

        var entity1 = new Entity("entity1");
        var positionComponent1 = new PositionComponent();
        entity1.AddComponent(positionComponent1);

        var entity2 = new Entity("entity2");
        var positionComponent2 = new PositionComponent();
        entity2.AddComponent(positionComponent2);

        context.Register(entity1);
        context.Register(entity2);

        // Act
        var view = context.View<PositionComponent>();

        // Assert
        view.Should().HaveCount(2);
        view.Should().ContainSingle(v => v.Item1 == entity1 && v.Item2 == positionComponent1);
        view.Should().ContainSingle(v => v.Item1 == entity2 && v.Item2 == positionComponent2);
    }

    [Fact]
    public void View_Should_Return_Empty_List_When_No_Entities_Have_The_Component()
    {
        // Arrange
        var context = Context.Instance;
        context.Entities.Clear(); // Clear the context for test isolation

        var entity1 = new Entity("entity1");
        entity1.AddComponent(new VelocityComponent());

        context.Register(entity1);

        // Act
        var view = context.View<PositionComponent>();

        // Assert
        view.Should().BeEmpty();
    }
}