namespace ECS.Tests;

/// <summary>
/// Tests for Entity component management functionality.
/// </summary>
public class EntityComponentTests
{
    [Fact]
    public void AddComponent_WithParameterlessConstructor_AddsComponent()
    {
        // Arrange
        var entity = Entity.Create(1, "TestEntity");

        // Act
        var component = entity.AddComponent<TestComponent>();

        // Assert
        Assert.NotNull(component);
        Assert.True(entity.HasComponent<TestComponent>());
    }

    [Fact]
    public void AddComponent_WithPreConstructedComponent_AddsComponent()
    {
        // Arrange
        var entity = Entity.Create(1, "TestEntity");
        var component = new TestComponentWithParams("TestValue", 42);

        // Act
        var result = entity.AddComponent(component);

        // Assert
        Assert.NotNull(result);
        Assert.Same(component, result);
        Assert.True(entity.HasComponent<TestComponentWithParams>());
        Assert.Equal("TestValue", result.Name);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void AddComponent_GenericWithPreConstructedComponent_AddsComponent()
    {
        // Arrange
        var entity = Entity.Create(1, "TestEntity");
        var component = new TestComponentWithParams("TestValue", 42);

        // Act
        var result = entity.AddComponent<TestComponentWithParams>(component);

        // Assert
        Assert.NotNull(result);
        Assert.Same(component, result);
        Assert.True(entity.HasComponent<TestComponentWithParams>());
        Assert.Equal("TestValue", result.Name);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void AddComponent_DuplicateWithParameterlessConstructor_ThrowsException()
    {
        // Arrange
        var entity = Entity.Create(1, "TestEntity");
        entity.AddComponent<TestComponent>();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            entity.AddComponent<TestComponent>());
        
        Assert.Contains("Entity 1", exception.Message);
        Assert.Contains("TestEntity", exception.Message);
        Assert.Contains("TestComponent", exception.Message);
        Assert.Contains("already has component", exception.Message);
    }

    [Fact]
    public void AddComponent_DuplicateWithPreConstructedComponent_ThrowsException()
    {
        // Arrange
        var entity = Entity.Create(1, "TestEntity");
        entity.AddComponent(new TestComponent());

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            entity.AddComponent(new TestComponent()));
        
        Assert.Contains("Entity 1", exception.Message);
        Assert.Contains("TestEntity", exception.Message);
        Assert.Contains("TestComponent", exception.Message);
        Assert.Contains("already has component", exception.Message);
    }

    [Fact]
    public void AddComponent_GenericDuplicateWithPreConstructedComponent_ThrowsException()
    {
        // Arrange
        var entity = Entity.Create(1, "TestEntity");
        entity.AddComponent<TestComponent>(new TestComponent());

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            entity.AddComponent<TestComponent>(new TestComponent()));
        
        Assert.Contains("Entity 1", exception.Message);
        Assert.Contains("TestEntity", exception.Message);
        Assert.Contains("TestComponent", exception.Message);
        Assert.Contains("already has component", exception.Message);
    }

    [Fact]
    public void AddComponent_MixedDuplicateParameterlessThenPreConstructed_ThrowsException()
    {
        // Arrange
        var entity = Entity.Create(1, "TestEntity");
        entity.AddComponent<TestComponent>();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            entity.AddComponent(new TestComponent()));
        
        Assert.Contains("already has component", exception.Message);
    }

    [Fact]
    public void AddComponent_MixedDuplicatePreConstructedThenParameterless_ThrowsException()
    {
        // Arrange
        var entity = Entity.Create(1, "TestEntity");
        entity.AddComponent(new TestComponent());

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            entity.AddComponent<TestComponent>());
        
        Assert.Contains("already has component", exception.Message);
    }

    [Fact]
    public void AddComponent_DifferentComponentTypes_BothAdded()
    {
        // Arrange
        var entity = Entity.Create(1, "TestEntity");

        // Act
        entity.AddComponent<TestComponent>();
        entity.AddComponent<AnotherTestComponent>();

        // Assert
        Assert.True(entity.HasComponent<TestComponent>());
        Assert.True(entity.HasComponent<AnotherTestComponent>());
    }

    [Fact]
    public void AddComponent_InvokesOnComponentAddedEvent()
    {
        // Arrange
        var entity = Entity.Create(1, "TestEntity");
        IComponent? addedComponent = null;
        entity.OnComponentAdded += (component) => addedComponent = component;

        // Act
        var component = entity.AddComponent<TestComponent>();

        // Assert
        Assert.NotNull(addedComponent);
        Assert.Same(component, addedComponent);
    }

    [Fact]
    public void AddComponent_WithPreConstructedComponent_InvokesOnComponentAddedEvent()
    {
        // Arrange
        var entity = Entity.Create(1, "TestEntity");
        IComponent? addedComponent = null;
        entity.OnComponentAdded += (component) => addedComponent = component;
        var component = new TestComponent();

        // Act
        entity.AddComponent(component);

        // Assert
        Assert.NotNull(addedComponent);
        Assert.Same(component, addedComponent);
    }

    [Fact]
    public void AddComponent_AfterRemovingComponent_CanAddAgain()
    {
        // Arrange
        var entity = Entity.Create(1, "TestEntity");
        entity.AddComponent<TestComponent>();
        entity.RemoveComponent<TestComponent>();

        // Act
        var component = entity.AddComponent<TestComponent>();

        // Assert
        Assert.NotNull(component);
        Assert.True(entity.HasComponent<TestComponent>());
    }

    [Fact]
    public void AddComponent_WithParameterizedConstructor_AllowsFluentInitialization()
    {
        // Arrange
        var entity = Entity.Create(1, "TestEntity");

        // Act
        var component = entity.AddComponent(new TestComponentWithParams("FluentTest", 100));

        // Assert
        Assert.Equal("FluentTest", component.Name);
        Assert.Equal(100, component.Value);
        Assert.True(entity.HasComponent<TestComponentWithParams>());
    }

    [Fact]
    public void AddComponent_WithDerivedComponentAsBaseType_StoresAsBaseType()
    {
        // Arrange
        var entity = Entity.Create(1, "TestEntity");
        var derivedComponent = new DerivedTestComponent();

        // Act
        entity.AddComponent<BaseTestComponent>(derivedComponent);

        // Assert
        Assert.True(entity.HasComponent<BaseTestComponent>());
        Assert.False(entity.HasComponent<DerivedTestComponent>());
    }

    [Fact]
    public void AddComponent_CanAddBothBaseAndDerivedTypes_WhenStoredAsDifferentTypes()
    {
        // Arrange
        var entity = Entity.Create(1, "TestEntity");

        // Act
        entity.AddComponent<BaseTestComponent>(new DerivedTestComponent());
        entity.AddComponent<DerivedTestComponent>(new DerivedTestComponent());

        // Assert
        Assert.True(entity.HasComponent<BaseTestComponent>());
        Assert.True(entity.HasComponent<DerivedTestComponent>());
    }

    [Fact]
    public void AddComponent_DuplicateBaseTypeWithDerivedInstance_ThrowsException()
    {
        // Arrange
        var entity = Entity.Create(1, "TestEntity");
        entity.AddComponent<BaseTestComponent>(new DerivedTestComponent());

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            entity.AddComponent<BaseTestComponent>(new DerivedTestComponent()));

        Assert.Contains("already has component", exception.Message);
        Assert.Contains("BaseTestComponent", exception.Message);
    }

    // Test components
    private class TestComponent : IComponent
    {
    }

    private class AnotherTestComponent : IComponent
    {
    }

    private class TestComponentWithParams : IComponent
    {
        public string Name { get; set; }
        public int Value { get; set; }

        public TestComponentWithParams(string name, int value)
        {
            Name = name;
            Value = value;
        }
    }

    private class BaseTestComponent : IComponent
    {
    }

    private class DerivedTestComponent : BaseTestComponent
    {
    }
}
