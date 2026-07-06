using Shouldly;

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
        component.ShouldNotBeNull();
        entity.HasComponent<TestComponent>().ShouldBeTrue();
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
        result.ShouldNotBeNull();
        result.ShouldBeSameAs(component);
        entity.HasComponent<TestComponentWithParams>().ShouldBeTrue();
        result.Name.ShouldBe("TestValue");
        result.Value.ShouldBe(42);
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
        result.ShouldNotBeNull();
        result.ShouldBeSameAs(component);
        entity.HasComponent<TestComponentWithParams>().ShouldBeTrue();
        result.Name.ShouldBe("TestValue");
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void AddComponent_DuplicateWithParameterlessConstructor_ThrowsException()
    {
        // Arrange
        var entity = Entity.Create(1, "TestEntity");
        entity.AddComponent<TestComponent>();

        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() => 
            entity.AddComponent<TestComponent>());
        
        exception.Message.ShouldContain("Entity 1");
        exception.Message.ShouldContain("TestEntity");
        exception.Message.ShouldContain("TestComponent");
        exception.Message.ShouldContain("already has component");
    }

    [Fact]
    public void AddComponent_DuplicateWithPreConstructedComponent_ThrowsException()
    {
        // Arrange
        var entity = Entity.Create(1, "TestEntity");
        entity.AddComponent(new TestComponent());

        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() => 
            entity.AddComponent(new TestComponent()));
        
        exception.Message.ShouldContain("Entity 1");
        exception.Message.ShouldContain("TestEntity");
        exception.Message.ShouldContain("TestComponent");
        exception.Message.ShouldContain("already has component");
    }

    [Fact]
    public void AddComponent_GenericDuplicateWithPreConstructedComponent_ThrowsException()
    {
        // Arrange
        var entity = Entity.Create(1, "TestEntity");
        entity.AddComponent<TestComponent>(new TestComponent());

        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() => 
            entity.AddComponent<TestComponent>(new TestComponent()));
        
        exception.Message.ShouldContain("Entity 1");
        exception.Message.ShouldContain("TestEntity");
        exception.Message.ShouldContain("TestComponent");
        exception.Message.ShouldContain("already has component");
    }

    [Fact]
    public void AddComponent_MixedDuplicateParameterlessThenPreConstructed_ThrowsException()
    {
        // Arrange
        var entity = Entity.Create(1, "TestEntity");
        entity.AddComponent<TestComponent>();

        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() => 
            entity.AddComponent(new TestComponent()));
        
        exception.Message.ShouldContain("already has component");
    }

    [Fact]
    public void AddComponent_MixedDuplicatePreConstructedThenParameterless_ThrowsException()
    {
        // Arrange
        var entity = Entity.Create(1, "TestEntity");
        entity.AddComponent(new TestComponent());

        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() => 
            entity.AddComponent<TestComponent>());
        
        exception.Message.ShouldContain("already has component");
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
        entity.HasComponent<TestComponent>().ShouldBeTrue();
        entity.HasComponent<AnotherTestComponent>().ShouldBeTrue();
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
        component.ShouldNotBeNull();
        entity.HasComponent<TestComponent>().ShouldBeTrue();
    }

    [Fact]
    public void AddComponent_WithParameterizedConstructor_AllowsFluentInitialization()
    {
        // Arrange
        var entity = Entity.Create(1, "TestEntity");

        // Act
        var component = entity.AddComponent(new TestComponentWithParams("FluentTest", 100));

        // Assert
        component.Name.ShouldBe("FluentTest");
        component.Value.ShouldBe(100);
        entity.HasComponent<TestComponentWithParams>().ShouldBeTrue();
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
        entity.HasComponent<BaseTestComponent>().ShouldBeTrue();
        entity.HasComponent<DerivedTestComponent>().ShouldBeFalse();
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
        entity.HasComponent<BaseTestComponent>().ShouldBeTrue();
        entity.HasComponent<DerivedTestComponent>().ShouldBeTrue();
    }

    [Fact]
    public void AddComponent_DuplicateBaseTypeWithDerivedInstance_ThrowsException()
    {
        // Arrange
        var entity = Entity.Create(1, "TestEntity");
        entity.AddComponent<BaseTestComponent>(new DerivedTestComponent());

        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() =>
            entity.AddComponent<BaseTestComponent>(new DerivedTestComponent()));

        exception.Message.ShouldContain("already has component");
        exception.Message.ShouldContain("BaseTestComponent");
    }

    // Test components
    private class TestComponent : IComponent
    {
        public IComponent Clone()
        {
            throw new NotImplementedException();
        }
    }

    private class AnotherTestComponent : IComponent
    {
        public IComponent Clone()
        {
            throw new NotImplementedException();
        }
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

        public IComponent Clone()
        {
            throw new NotImplementedException();
        }
    }

    private class BaseTestComponent : IComponent
    {
        public IComponent Clone()
        {
            throw new NotImplementedException();
        }
    }

    private class DerivedTestComponent : BaseTestComponent
    {
    }
}
